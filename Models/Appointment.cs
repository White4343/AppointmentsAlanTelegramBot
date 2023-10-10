using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlanTelegramBotApp.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int PatientId { get; set; }
    }
}
