using System.Text.Json;

namespace SeminarClassesAssistant.BOT.FileServices;

public static class QuestionsReader
{
    public static List<string> ReadQuestionsFromFile(string questionsFile)
    {
        try
        {
            if (!File.Exists(questionsFile))
            {
                Console.WriteLine($"Файл {questionsFile} не знайдено.");
                return new List<string>();
            }

            string jsonContent = File.ReadAllText(questionsFile);
            var loadedQuestions = JsonSerializer.Deserialize<List<string>>(jsonContent);

            Console.WriteLine($"Завантажено {loadedQuestions?.Count ?? 0} питань з {questionsFile}");
            return loadedQuestions ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка завантаження питань: {ex.Message}");
            return new List<string>();
        }
    }
}