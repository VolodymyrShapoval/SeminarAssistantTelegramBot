namespace SeminarClassesAssistant.BOT.QuestionServices;

public static class QuestionSorter
{
    public static List<string> Sort(List<string> questions)
    {
        return questions
            .OrderBy(q => GetSortKey(q))
            .ToList();
    }
    
    private static int GetSortKey(string question)
    {
        var match = System.Text.RegularExpressions.Regex.Match(question, @"^(\d+)\.(\d+)");
        
        if (match.Success && 
            int.TryParse(match.Groups[1].Value, out int topic) && 
            int.TryParse(match.Groups[2].Value, out int subtopic))
        {
            // 1.5 → 1005, 2.12 → 2012
            return topic * 1000 + subtopic;
        }
        
        return 999999; // Питання без правильного формату в кінець
    }
}