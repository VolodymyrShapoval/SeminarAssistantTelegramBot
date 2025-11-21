using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SeminarClassesAssistant.BOT.QuestionServices;

public static class QuestionsUpdater
{
    public static async Task UpdateAllQuestionsInUsers(ITelegramBotClient client, Dictionary<ChatId, int> userQuestionMessageIds, List<string> questions)
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
}