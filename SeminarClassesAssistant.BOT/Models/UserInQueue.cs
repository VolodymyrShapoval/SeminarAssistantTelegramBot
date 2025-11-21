namespace SeminarClassesAssistant.BOT.Models
{
    class UserInQueue : IComparable<UserInQueue>
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string QuestionNumberStr { get; set; } = "0"; // Зберігаємо як "1.1", "2.3"
        public DateTime SelectedAt { get; set; }

        // Допоміжна властивість для сортування
        public int QuestionNumber => int.TryParse(QuestionNumberStr.Split('.')[0], out var num) ? num : 0;

        // Сортування по номеру питання
        public int CompareTo(UserInQueue? other)
        {
            if (other == null) return 1;
            
            // Спочатку по темі (1.x, 2.x), потім по підномеру
            var parts1 = QuestionNumberStr.Split('.');
            var parts2 = other.QuestionNumberStr.Split('.');
            
            // Порівнюємо тему (перша цифра)
            if (parts1.Length > 0 && parts2.Length > 0)
            {
                int topic1 = int.TryParse(parts1[0], out var t1) ? t1 : 0;
                int topic2 = int.TryParse(parts2[0], out var t2) ? t2 : 0;
                
                if (topic1 != topic2)
                    return topic1.CompareTo(topic2);
                
                // Якщо теми однакові, порівнюємо підномер (друга цифра)
                if (parts1.Length > 1 && parts2.Length > 1)
                {
                    int sub1 = int.TryParse(parts1[1], out var s1) ? s1 : 0;
                    int sub2 = int.TryParse(parts2[1], out var s2) ? s2 : 0;
                    return sub1.CompareTo(sub2);
                }
            }
            
            return 0;
        }
    }
}
