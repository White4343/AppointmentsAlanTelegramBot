using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlanTelegramBotApp.Models
{
    public class TelegramMessage
    {
        public int MessageId { get; set; }
        public required string Content { get; set; }
        public long TelegramId { get; set; }
    }
}
