using System.Collections.Generic;

namespace KuaforRandevuSistemi.Models
{
    public class AppointmentViewModel
    {
        public List<Appointment> UpcomingAppointments { get; set; }
        public List<Appointment> PastAppointments { get; set; }
        public List<Appointment> CancelledAppointments { get; set; }
    }
}
