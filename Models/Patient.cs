using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlanTelegramBotApp.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public required string Diagnosis { get; set; }
        public required string FullName { get; set; }
        public required string BirthDate { get; set; }
        public long TelegramId { get; set; }
    }
}