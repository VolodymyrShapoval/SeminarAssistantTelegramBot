namespace SeminarClassesAssistant.BOT.Models
{
    public class UserSession
    {
        public long UserId { get; set; }
        public string Role { get; set; } = "user";
        public bool IsLoggedIn { get; set; } = false;
    }
}
