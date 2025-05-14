using System.ComponentModel.DataAnnotations;

namespace KuaforRandevuSistemi.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int StaffId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        public Staff? Staff { get; set; }
        public Service? Service { get; set; }
        public bool IsCancelled { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}