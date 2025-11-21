using System.Text.Json;
using SeminarClassesAssistant.BOT.Models;

namespace SeminarClassesAssistant.BOT.QuestionServices;

public static class UserQueueRemover
{
    public static async Task RemoveUserFromQueue(long userId, string queueFile)
    {
        try
        {
            if (!File.Exists(queueFile))
            {
                Console.WriteLine($"Файл {queueFile} не існує.");
                return;
            }

            string existingContent = await File.ReadAllTextAsync(queueFile);
            
            if (string.IsNullOrWhiteSpace(existingContent))
            {
                Console.WriteLine("Файл черги порожній.");
                return;
            }

            var queue = JsonSerializer.Deserialize<List<UserInQueue>>(existingContent) ?? new();
            
            // Видаляємо всі записи цього користувача по UserId
            int removedCount = queue.RemoveAll(q => q.UserId == userId);
            
            if (removedCount == 0)
            {
                Console.WriteLine($"Користувач {userId} не знайдений в черзі.");
                return;
            }
            
            // Зберігаємо оновлену чергу
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonContent = JsonSerializer.Serialize(queue, options);
            await File.WriteAllTextAsync(queueFile, jsonContent);

            Console.WriteLine($"Користувача {userId} видалено з черги ({queueFile}). Видалено записів: {removedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка видалення з черги: {ex.Message}");
        }
    }
}