using SeminarClassesAssistant.BOT.Models;
using System.Collections.Concurrent;
using SeminarClassesAssistant.BOT.FileServices;
using SeminarClassesAssistant.BOT.QuestionServices;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

const string ACCESS_PASSWORD = "seminar2025";
const string QUESTIONS_FILE = "questions.json";
const string QUEUE_FILE = "queue.json";
FileCleaner.ClearFile(QUEUE_FILE);

ConcurrentDictionary<long, UserSession> users = new();
Dictionary<ChatId, string> userQuestions = new();
Dictionary<ChatId, int> userQuestionMessageIds = new();

List<string> questions = QuestionsReader.ReadQuestionsFromFile(questionsFile: QUESTIONS_FILE);

TelegramBotClient botClient = new(token: "8484504732:AAE3x1wnixzzBqWN0Xg6RU6lHUQRVVEMBng");

// Запуск прийому оновлень
try
{
    botClient.StartReceiving(Update, Error);
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex}. Втрачено з‘єднання");
}

await Task.Delay(-1);

// ===========================================================
// ГОЛОВНИЙ ОБРОБНИК UPDATE
// ===========================================================
async Task Update(ITelegramBotClient сlient, Update update, CancellationToken token)
{
    // 🔹 Обробка повідомлень (Message)
    if (update.Message is { } message && message.Text is { } messageText)
    {
        long userId = message.Chat.Id;

        if (!users.ContainsKey(userId))
            users[userId] = new UserSession { UserId = userId };

        var session = users[userId];

        // ---------- Команда /start ----------
        if (messageText == "/start")
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🔐 Увійти в сесію" }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: userId,
                text: "Привіт 👋 Натисни 'Увійти в сесію' і введи пароль.",
                replyMarkup: keyboard,
                cancellationToken: token
            );
            return;
        }

        // ---------- Натискання кнопки входу ----------
        if (messageText == "/login")
        {
            if (session.IsLoggedIn)
            {
                await botClient.SendMessage(userId, "Ви вже авторизовані!");
                return;
            }
            await botClient.SendMessage(userId, "Введіть пароль:");
            return;
        }

        // ---------- Авторизація ----------
        if (messageText == ACCESS_PASSWORD)
        {
            session.IsLoggedIn = true;
            session.Role = "user";

            var removeKeyboard = new ReplyKeyboardRemove();

            await botClient.SendMessage(
                chatId: userId,
                text: "✅ Ви увійшли як *учасник* семінару.",
                parseMode: ParseMode.Markdown,
                replyMarkup: removeKeyboard
            );

            var messageId = await QuestionPresenter.ShowQuestions(botClient, userId, questions);
            userQuestionMessageIds[userId] = messageId;
            return;
        }

        // ---------- Якщо користувач не ввійшов ----------
        if (!session.IsLoggedIn)
        {
            await botClient.SendMessage(userId, "Введіть пароль, щоб увійти в сесію.");
            return;
        }

        if (messageText == "/showquestions")
        {
            var messageId = await QuestionPresenter.ShowQuestions(botClient, userId, questions);
            userQuestionMessageIds[userId] = messageId;
            return;
        }

        if (messageText == "/myquestion")
        {
            if (userQuestions.ContainsKey(userId))
            {
                string question = userQuestions[userId];
                await botClient.SendMessage(userId,
                                            $"Ваше запитання: {question}");
            }
            else
            {
                await botClient.SendMessage(userId, "Ви ще не обрали питання.");
            }
            return;
        }
        
        // При скасуванні питання
        if (messageText == "/cancelmyquestion")
        {
            if (!userQuestions.ContainsKey(userId))
            {
                await botClient.SendMessage(userId, "Ви не обирали жодного питання.");
                return;
            }

            string canceledQuestion = userQuestions[userId];
    
            userQuestions.Remove(userId);
            questions.Add(canceledQuestion);
            questions = QuestionSorter.Sort(questions);
    
            // Тепер не потрібно передавати users
            await UserQueueRemover.RemoveUserFromQueue(userId, QUEUE_FILE);
    
            await botClient.SendMessage(userId, $"❌ Ви скасували питання:\n{canceledQuestion}\n\nТепер воно знову доступне для інших.");
    
            await QuestionsUpdater.UpdateAllQuestionsInUsers(botClient, userQuestionMessageIds, questions);
    
            return;
        }

        if (messageText == "/showqueue")
        {
            await QuestionsWithUsersQueuePresenter.ShowQuestionsWithUsersQueue(botClient, userId, QUEUE_FILE);
            return;
        }
    }

    // =======================================================
    // 🔹 Обробка CallbackQuery (натискання на inline-кнопку)
    // =======================================================
    if (update.CallbackQuery is { } callbackQuery)
    {
        var data = callbackQuery.Data;
        var user = callbackQuery.From;
        long chatId = callbackQuery.Message.Chat.Id;

        if (!users.ContainsKey(chatId) || !users[chatId].IsLoggedIn)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "⛔ Спочатку увійди в сесію!");
            return;
        }

        // Якщо натиснута кнопка питання
        if (data.StartsWith("question_"))
        {
            int index = int.Parse(data.Split('_')[1]);

            // Якщо питання вже вибране іншим (індекс вийшов за межі)
            if (index >= questions.Count)
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "❗ Це питання вже вибрали інші.");
                return;
            }

            string selectedQuestion = questions[index];

            // Якщо користувач уже вибрав питання
            if (userQuestions.ContainsKey(user.Id))
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ви вже обрали питання!");
                return;
            }

            // Закріпити питання за користувачем
            userQuestions[user.Id] = selectedQuestion;

            // Передаємо userId, username і question
            await UsersWithQuestionsWriter.AddUserWithQuestionToJSON(
                user.Id, 
                user.Username ?? string.Empty, 
                selectedQuestion, 
                QUEUE_FILE
            );
            
            // Видалити питання зі списку
            questions.RemoveAt(index);

            // Повідомити користувача
            await botClient.AnswerCallbackQuery(callbackQuery.Id, $"✅ Ви обрали: {selectedQuestion}");

            // ОНОВИТИ СПИСОК У ВСІХ КОРИСТУВАЧІВ
            await QuestionsUpdater.UpdateAllQuestionsInUsers(botClient, userQuestionMessageIds, questions);
        }
    }
}

// ===========================================================
// ОБРОБКА ПОМИЛОК
// ===========================================================
Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
{
    Console.WriteLine($"Помилка: {exception.Message}");
    return Task.CompletedTask;
}