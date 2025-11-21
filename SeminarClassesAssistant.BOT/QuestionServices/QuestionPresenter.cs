using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SeminarClassesAssistant.BOT.QuestionServices;

public static class QuestionPresenter
{
    public static async Task<int> ShowQuestions(ITelegramBotClient client, ChatId chatId, List<string> questions)
    {
        if (questions.Count == 0)
        {
            await client.SendMessage(chatId, "Немає доступних питань 😕");
            return 0;
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
        return sentMessage.MessageId;
    }
}