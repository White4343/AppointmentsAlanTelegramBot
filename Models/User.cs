    using System.ComponentModel.DataAnnotations;

    namespace AlanTelegramBotApp.Models
{
    public class User
    {
        public long TelegramId { get; set; }
        public string? FullName { get; set; }
        public string? TelephoneNumber { get; set; }
        public string? City { get; set; }
        public bool? IsAdmin { get; set; }
    }
}
