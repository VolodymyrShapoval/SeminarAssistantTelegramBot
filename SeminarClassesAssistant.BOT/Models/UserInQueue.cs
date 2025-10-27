namespace SeminarClassesAssistant.BOT.Models
{
    class UserInQueue : IComparable<UserInQueue>
    {
        public string Username { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public int QuestionNumber { get; set; }
        public DateTime SelectedAt { get; set; }

        public int CompareTo(UserInQueue? other)
        {
            if (other == null) return 1;
            return QuestionNumber.CompareTo(other.QuestionNumber);
        }
    }
}
