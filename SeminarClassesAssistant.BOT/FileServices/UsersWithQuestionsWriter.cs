using System.Text.Json;
using SeminarClassesAssistant.BOT.Models;

namespace SeminarClassesAssistant.BOT.FileServices;

public static class UsersWithQuestionsWriter
{
    public static async Task AddUserWithQuestionToJSON(long userId, string username, string question, string queueFile)
    {
        try
        {
            // Читаємо існуючу чергу
            List<UserInQueue> queue = new();

            if (File.Exists(queueFile))
            {
                string existingContent = await File.ReadAllTextAsync(queueFile);
                queue = JsonSerializer.Deserialize<List<UserInQueue>>(existingContent) ?? new();
            }

            // Витягуємо номер питання (1.1 → "1.1", 2.3 → "2.3")
            string questionNumber = "0";
            var match = System.Text.RegularExpressions.Regex.Match(question, @"^([\d.]+)");
            if (match.Success)
            {
                questionNumber = match.Groups[1].Value;
            }
            
            string displayUsername = string.IsNullOrWhiteSpace(username) 
                ? $"User{userId}" 
                : username;

            // Додаємо нового користувача
            queue.Add(new UserInQueue
            {
                UserId = userId,
                Username = username,
                Question = question,
                QuestionNumberStr = questionNumber, // Змінено: використовуємо QuestionNumberStr
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
            await File.WriteAllTextAsync(queueFile, jsonContent);

            Console.WriteLine($"Користувача {displayUsername} (ID: {userId}) додано до черги ({queueFile})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка додавання до черги: {ex.Message}");
        }
    }
}