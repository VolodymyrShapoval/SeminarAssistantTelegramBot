using SeminarClassesAssistant.BOT.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

const string ACCESS_PASSWORD = "seminar2025";
const string QUESTIONS_FILE = "questions.json";
const string QUEUE_FILE = "queue.json";
ClearQueueFile();

ConcurrentDictionary<long, UserSession> users = new();
Dictionary<ChatId, string> userQuestions = new();
Dictionary<ChatId, int> userQuestionMessageIds = new();

List<string> questions = LoadQuestions();

TelegramBotClient botClient = new("8484504732:AAE3x1wnixzzBqWN0Xg6RU6lHUQRVVEMBng");

// Запуск прийому оновлень
botClient.StartReceiving(Update, Error);
Console.ReadLine();

// ===========================================================
// ГОЛОВНИЙ ОБРОБНИК UPDATE
// ===========================================================
async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
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

            await ShowOptions(botClient, userId);
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
            await ShowOptions(botClient, userId);
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

        if (messageText == "/showqueue")
        {
            await ShowQuestionsUsersQueue(botClient, userId);
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

            await AddUserWithQuestionToJSON(user.Username!, selectedQuestion);

            // Видалити питання зі списку
            questions.RemoveAt(index);

            // Повідомити користувача
            await botClient.AnswerCallbackQuery(callbackQuery.Id, $"✅ Ви обрали: {selectedQuestion}");

            // 🔥 ОНОВИТИ СПИСОК У ВСІХ КОРИСТУВАЧІВ
            await UpdateAllUsersQuestionLists(botClient);
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

// ===========================================================
// ВИВЕДЕННЯ ПИТАНЬ
// ===========================================================
async Task ShowOptions(ITelegramBotClient client, ChatId chatId)
{
    if (questions.Count == 0)
    {
        await client.SendMessage(chatId, "Немає доступних питань 😕");
        return;
    }

    var inlineKeyboard = new InlineKeyboardMarkup(
        questions.Select((q, index) =>
            new[]
            {
                InlineKeyboardButton.WithCallbackData(q, $"question_{index}")
            })
    );

    var sentMessage = await client.SendMessage(
        chatId: chatId,
        text: "🧾 Оберіть питання для семінару:",
        replyMarkup: inlineKeyboard
    );

    // Зберігаємо messageId для подальшого оновлення
    userQuestionMessageIds[chatId] = sentMessage.MessageId;
}

// ===========================================================
// ВИВЕДЕННЯ ВСІХ ПИТАНЬ ТА КОРИСТУВАЧІВ, ЯКІ ЇХ ОБРАЛИ
// ===========================================================
async Task ShowQuestionsUsersQueue(ITelegramBotClient client, ChatId chatId)
{
    try
    {
        if (!File.Exists(QUEUE_FILE))
        {
            await client.SendMessage(
                chatId: chatId,
                text: "⚠️ Ще ніхто не обрав запитання!");
            return;
        }

        string existingContent = await File.ReadAllTextAsync(QUEUE_FILE);
        var queue = JsonSerializer.Deserialize<List<UserInQueue>>(existingContent);

        if (queue == null || queue.Count == 0)
        {
            await client.SendMessage(
                chatId: chatId,
                text: "Ще ніхто не обрав запитання!");
            return;
        }

        // Сортуємо по номеру питання
        queue.Sort();

        // Формуємо текст повідомлення
        var messageText = "📋 *Черга виступів:*\n\n";

        foreach (var user in queue)
        {
            messageText += $"🔹 Питання {user.QuestionNumber}\n";
            messageText += $"   {user.Question}\n";
            messageText += $"   👤 @{user.Username}\n";
            messageText += $"   🕐 {user.SelectedAt:dd.MM.yyyy HH:mm}\n\n";
        }

        await client.SendMessage(
            chatId: chatId,
            text: messageText);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка завантаження списку: {ex.Message}");
        await client.SendMessage(
            chatId: chatId,
            text: "Помилка при завантаженні черги 😕");
    }
}


// ===========================================================
// ОНОВЛЕННЯ СПИСКУ У ВСІХ КОРИСТУВАЧІВ
// ===========================================================
async Task UpdateAllUsersQuestionLists(ITelegramBotClient client)
{
    // Проходимо по всіх користувачах, які мають збережене повідомлення зі списком
    foreach (var kvp in userQuestionMessageIds.ToList())
    {
        ChatId chatId = kvp.Key;
        int messageId = kvp.Value;

        try
        {
            if (questions.Count == 0)
            {
                await client.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "✅ Усі питання вже розібрано!"
                );
                // Видаляємо з словника, бо більше не треба оновлювати
                userQuestionMessageIds.Remove(chatId);
            }
            else
            {
                var inlineKeyboard = new InlineKeyboardMarkup(
                    questions.Select((q, index) =>
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(q, $"question_{index}")
                        })
                );

                await client.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: "🧾 Оберіть питання для семінару:",
                    replyMarkup: inlineKeyboard
                );
            }
        }
        catch (Exception ex)
        {
            // Якщо повідомлення видалено або недоступне
            Console.WriteLine($"Не вдалося оновити список для користувача {chatId}: {ex.Message}");
            userQuestionMessageIds.Remove(chatId);
        }
    }
}

// ===========================================================
// ЗАВАНТАЖЕННЯ ПИТАНЬ З JSON
// ===========================================================
List<string> LoadQuestions()
{
    try
    {
        if (!File.Exists(QUESTIONS_FILE))
        {
            Console.WriteLine($"Файл {QUESTIONS_FILE} не знайдено.");
            return new List<string>();
        }

        string jsonContent = File.ReadAllText(QUESTIONS_FILE);
        var loadedQuestions = JsonSerializer.Deserialize<List<string>>(jsonContent);

        Console.WriteLine($"Завантажено {loadedQuestions?.Count ?? 0} питань з {QUESTIONS_FILE}");
        return loadedQuestions ?? new List<string>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка завантаження питань: {ex.Message}");
        return new List<string>();
    }
}

// ===========================================================
// ДОДАВАННЯ КОРИСТУВАЧА ДО ЧЕРГИ У JSON
// ===========================================================
async Task AddUserWithQuestionToJSON(string username, string question)
{
    try
    {
        // Читаємо існуючу чергу
        List<UserInQueue> queue = new();

        if (File.Exists(QUEUE_FILE))
        {
            string existingContent = await File.ReadAllTextAsync(QUEUE_FILE);
            queue = JsonSerializer.Deserialize<List<UserInQueue>>(existingContent) ?? new();
        }

        // Витягуємо номер питання
        int questionNumber = 0;
        var match = System.Text.RegularExpressions.Regex.Match(question, @"^(\d+)\.");
        if (match.Success)
        {
            questionNumber = int.Parse(match.Groups[1].Value);
        }

        // Додаємо нового користувача
        queue.Add(new UserInQueue
        {
            Username = username,
            Question = question,
            QuestionNumber = questionNumber,
            SelectedAt = DateTime.Now
        });
        queue.Sort();

        // Зберігаємо оновлену чергу
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string jsonContent = JsonSerializer.Serialize(queue, options);
        await File.WriteAllTextAsync(QUEUE_FILE, jsonContent);

        Console.WriteLine($"Користувача {username} додано до черги ({QUEUE_FILE})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка додавання до черги: {ex.Message}");
    }
}

// ===========================================================
// ОЧИЩЕННЯ ФАЙЛУ ЧЕРГИ ПРИ ЗАПУСКУ
// ===========================================================
void ClearQueueFile()
{
    try
    {
        if (File.Exists(QUEUE_FILE))
        {
            File.Delete(QUEUE_FILE);
            Console.WriteLine($"Файл {QUEUE_FILE} видалено");
        }

        // Створюємо порожній масив JSON
        Console.WriteLine($"Файл {QUEUE_FILE} створено порожнім");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка очищення файлу черги: {ex.Message}");
    }
}