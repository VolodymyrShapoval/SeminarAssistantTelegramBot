namespace SeminarClassesAssistant.BOT.FileServices;

public static class FileCleaner
{
    public static void ClearFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Console.WriteLine($"Файл {path} видалено");
            }

            // Створюємо порожній масив JSON
            Console.WriteLine($"Файл {path} створено порожнім");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка очищення файлу: {ex.Message}");
        }
    }
}