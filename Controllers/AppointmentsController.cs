using KuaforRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KuaforRandevuSistemi.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KuaforRandevuSistemi.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var upcomingAppointments = await _context.Appointments
                    .Include(a => a.Staff)
                    .Include(a => a.Service)
                    .Where(a => a.UserId == userId && a.AppointmentDate > DateTime.Now && !a.IsCancelled)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();

                var pastAppointments = await _context.Appointments
                    .Include(a => a.Staff)
                    .Include(a => a.Service)
                    .Where(a => a.UserId == userId && a.AppointmentDate <= DateTime.Now && !a.IsCancelled)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                var cancelledAppointments = await _context.Appointments
                    .Include(a => a.Staff)
                    .Include(a => a.Service)
                    .Where(a => a.UserId == userId && a.IsCancelled)
                    .ToListAsync();

                foreach (var appointment in upcomingAppointments)
                {
                    Console.WriteLine($"Randevu ID: {appointment.Id}, " +
                                      $"Staff: {(appointment.Staff != null ? appointment.Staff.Name : "null")}, " +
                                      $"Service: {(appointment.Service != null ? appointment.Service.Name : "null")}");
                }

                return View(new AppointmentViewModel
                {
                    UpcomingAppointments = upcomingAppointments,
                    PastAppointments = pastAppointments,
                    CancelledAppointments = cancelledAppointments
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Randevu listesi yüklenirken hata: {ex.Message}");
                return View(new AppointmentViewModel
                {
                    UpcomingAppointments = new List<Appointment>(),
                    PastAppointments = new List<Appointment>(),
                    CancelledAppointments = new List<Appointment>()
                });
            }
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name");
            ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            Console.WriteLine($"Form verileri - StaffId: {appointment.StaffId}, ServiceId: {appointment.ServiceId}, UserId: {appointment.UserId}");

            try
            {
                if (appointment == null)
                {
                    ModelState.AddModelError("", "Hatalı form gönderimi");
                    ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name");
                    ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name");
                    return View(appointment);
                }

                if (string.IsNullOrEmpty(appointment.UserId))
                {
                    var userId = _userManager.GetUserId(User);
                    if (string.IsNullOrEmpty(userId))
                    {
                        ModelState.AddModelError("UserId", "Kullanıcı kimliği alınamadı. Lütfen tekrar giriş yapın.");
                        ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                        ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                        return View(appointment);
                    }
                    appointment.UserId = userId;
                }

                var actualUserId = _userManager.GetUserId(User);
                if (appointment.UserId != actualUserId)
                {
                    appointment.UserId = actualUserId;
                }

                if (appointment.AppointmentDate < DateTime.Now)
                {
                    ModelState.AddModelError("AppointmentDate", "Geçmiş tarihli randevu oluşturulamaz");
                }

                if (appointment.StaffId == 0)
                {
                    ModelState.AddModelError("StaffId", "Lütfen bir personel seçiniz");
                }

                if (appointment.ServiceId == 0)
                {
                    ModelState.AddModelError("ServiceId", "Lütfen bir hizmet seçiniz");
                }

                if (ModelState.ContainsKey("UserId"))
                {
                    ModelState["UserId"].Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    var staff = await _context.Staffs.FindAsync(appointment.StaffId);
                    var service = await _context.Services.FindAsync(appointment.ServiceId);

                    if (staff == null || service == null)
                    {
                        ModelState.AddModelError("", "Seçilen personel veya hizmet bulunamadı");
                        ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                        ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                        return View(appointment);
                    }

                    var hasConflict = await _context.Appointments
                        .AnyAsync(a => a.StaffId == appointment.StaffId &&
                                       a.AppointmentDate == appointment.AppointmentDate &&
                                       !a.IsCancelled);

                    if (hasConflict)
                    {
                        ModelState.AddModelError("", "Bu personel için seçilen saatte başka bir randevu bulunmaktadır");
                        ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                        ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                        return View(appointment);
                    }

                    appointment.CreatedAt = DateTime.Now;
                    appointment.IsCancelled = false;

                    appointment.Staff = staff;
                    appointment.Service = service;

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            Console.WriteLine($"Model validation error: {error.ErrorMessage}");
                        }
                    }
                    ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                    ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                    return View(appointment);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Randevu oluşturma hatası: {ex.Message}");
                ModelState.AddModelError("", "Randevu oluşturulurken bir hata oluştu: " + ex.Message);
                ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                return View(appointment);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
            ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            appointment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAppointment = await _context.Appointments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (existingAppointment == null)
                    {
                        return NotFound();
                    }

                    if (existingAppointment.UserId != appointment.UserId)
                    {
                        return Forbid();
                    }

                    appointment.CreatedAt = existingAppointment.CreatedAt;

                    var hasConflict = await _context.Appointments
                        .AnyAsync(a => a.Id != id &&
                                       a.StaffId == appointment.StaffId &&
                                       a.AppointmentDate == appointment.AppointmentDate &&
                                       !a.IsCancelled);

                    if (hasConflict)
                    {
                        ModelState.AddModelError("", "Bu personel için seçilen saatte başka bir randevu bulunmaktadır");
                        ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
                        ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
                        return View(appointment);
                    }

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Randevunuz başarıyla güncellendi";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Staffs = new SelectList(await _context.Staffs.ToListAsync(), "Id", "Name", appointment.StaffId);
            ViewBag.Services = new SelectList(await _context.Services.ToListAsync(), "Id", "Name", appointment.ServiceId);
            return View(appointment);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Staff)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                appointment.IsCancelled = true;
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}
