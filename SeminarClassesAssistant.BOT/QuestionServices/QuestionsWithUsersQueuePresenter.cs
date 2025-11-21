using System.Text.Json;
using SeminarClassesAssistant.BOT.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SeminarClassesAssistant.BOT.QuestionServices;

public static class QuestionsWithUsersQueuePresenter
{
    public static async Task ShowQuestionsWithUsersQueue(ITelegramBotClient client, ChatId chatId, string queueFile)
    {
        try
        {
            if (!File.Exists(queueFile))
            {
                await client.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Ще ніхто не обрав запитання!");
                return;
            }

            string existingContent = await File.ReadAllTextAsync(queueFile);
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
                messageText += $"🔹 Тема {user.QuestionNumber}\n";
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
}