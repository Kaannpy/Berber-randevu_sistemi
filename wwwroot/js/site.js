// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


const KuaforApp = {
    
    elements: {
        get: function(selector) {
            return document.querySelector(selector);
        },
        getAll: function(selector) {
            return document.querySelectorAll(selector);
        }
    },
    
    animations: {
        fadeIn: function(element, duration = 500) {
            element.style.opacity = 0;
            element.style.display = 'block';
            
            let start = null;
            function step(timestamp) {
                if (!start) start = timestamp;
                const progress = timestamp - start;
                element.style.opacity = Math.min(progress / duration, 1);
                if (progress < duration) {
                    window.requestAnimationFrame(step);
                }
            }
            window.requestAnimationFrame(step);
        },
        
        fadeOut: function(element, duration = 500) {
            element.style.opacity = 1;
            
            let start = null;
            function step(timestamp) {
                if (!start) start = timestamp;
                const progress = timestamp - start;
                element.style.opacity = Math.max(1 - progress / duration, 0);
                if (progress < duration) {
                    window.requestAnimationFrame(step);
                } else {
                    element.style.display = 'none';
                }
            }
            window.requestAnimationFrame(step);
        },
        
        slideDown: function(element, duration = 500) {
            element.style.display = 'block';
            const height = element.scrollHeight;
            
            element.style.overflow = 'hidden';
            element.style.height = '0px';
            element.style.paddingTop = '0px';
            element.style.paddingBottom = '0px';
            element.style.marginTop = '0px';
            element.style.marginBottom = '0px';
            
            setTimeout(() => {
                element.style.transition = `height ${duration}ms ease-out, 
                                           padding ${duration}ms ease-out, 
                                           margin ${duration}ms ease-out`;
                element.style.height = height + 'px';
                element.style.paddingTop = '';
                element.style.paddingBottom = '';
                element.style.marginTop = '';
                element.style.marginBottom = '';
            }, 10);
            
            setTimeout(() => {
                element.style.transition = '';
                element.style.height = '';
                element.style.overflow = '';
            }, duration + 10);
        }
    },
    
    appointments: {
        getAvailableSlots: function(staffId, date, callback) {
            const availableSlots = [];
            
            const startHour = 10;
            const endHour = 21;
            
            for (let hour = startHour; hour < endHour; hour++) {
                availableSlots.push(`${hour}:00`);
                availableSlots.push(`${hour}:30`);
            }
            
            const randomlyBookedSlots = Math.floor(Math.random() * 6) + 3;
            for (let i = 0; i < randomlyBookedSlots; i++) {
                const randomIndex = Math.floor(Math.random() * availableSlots.length);
                availableSlots.splice(randomIndex, 1);
            }
            
            if (typeof callback === 'function') {
                setTimeout(() => callback(availableSlots), 300); 
            }
            
            return availableSlots;
        },
        
        handleDateSelection: function() {
            const staffSelect = KuaforApp.elements.get('#StaffId');
            const dateInput = KuaforApp.elements.get('#AppointmentDate');
            const timeSlotContainer = KuaforApp.elements.get('#timeSlotContainer');
            
            if (staffSelect && dateInput && timeSlotContainer) {
                const staffId = staffSelect.value;
                const selectedDate = dateInput.value ? new Date(dateInput.value) : null;
                
                if (staffId && selectedDate) {
                    const dateOnly = selectedDate.toISOString().split('T')[0];
                    
                    KuaforApp.appointments.getAvailableSlots(staffId, dateOnly, function(slots) {
                        let html = '<div class="mt-3 mb-3"><h5>Müsait Saatler</h5>';
                        html += '<div class="time-slots d-flex flex-wrap gap-2">';
                        
                        slots.forEach(slot => {
                            html += `<button type="button" class="btn btn-outline-primary btn-sm time-slot-btn" data-time="${slot}">${slot}</button>`;
                        });
                        
                        html += '</div></div>';
                        
                        timeSlotContainer.innerHTML = html;
                        KuaforApp.animations.fadeIn(timeSlotContainer);
                        
                        const slotButtons = KuaforApp.elements.getAll('.time-slot-btn');
                        slotButtons.forEach(btn => {
                            btn.addEventListener('click', function() {
                                slotButtons.forEach(b => b.classList.remove('active', 'btn-primary'));
                                btn.classList.add('active', 'btn-primary');
                                btn.classList.remove('btn-outline-primary');
                                
                                const selectedTime = btn.getAttribute('data-time');
                                const [hours, minutes] = selectedTime.split(':');
                                
                                const newDate = new Date(selectedDate);
                                newDate.setHours(parseInt(hours, 10));
                                newDate.setMinutes(parseInt(minutes, 10));
                                newDate.setSeconds(0);
                                
                                // Create a locale-aware date string in ISO format that preserves the selected time
                                // Format: YYYY-MM-DDThh:mm
                                const year = newDate.getFullYear();
                                const month = String(newDate.getMonth() + 1).padStart(2, '0');
                                const day = String(newDate.getDate()).padStart(2, '0');
                                const formattedHours = String(parseInt(hours, 10)).padStart(2, '0');
                                const formattedMinutes = String(parseInt(minutes, 10)).padStart(2, '0');
                                
                                const formattedDate = `${year}-${month}-${day}T${formattedHours}:${formattedMinutes}`;
                                dateInput.value = formattedDate;
                            });
                        });
                    });
                }
            }
        },
        
        initAppointmentForm: function() {
            const staffSelect = KuaforApp.elements.get('#StaffId');
            const dateInput = KuaforApp.elements.get('#AppointmentDate');
            
            if (staffSelect && dateInput) {
                let timeSlotContainer = KuaforApp.elements.get('#timeSlotContainer');
                if (!timeSlotContainer) {
                    timeSlotContainer = document.createElement('div');
                    timeSlotContainer.id = 'timeSlotContainer';
                    dateInput.parentNode.insertAdjacentElement('afterend', timeSlotContainer);
                }
                
                staffSelect.addEventListener('change', KuaforApp.appointments.handleDateSelection);
                dateInput.addEventListener('change', KuaforApp.appointments.handleDateSelection);
            }
        }
    },
    
    ui: {
        initTooltips: function() {
            if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl);
                });
            }
        },
        
        initDropdowns: function() {
            const dropdowns = KuaforApp.elements.getAll('.custom-dropdown');
            dropdowns.forEach(dropdown => {
                const trigger = dropdown.querySelector('.dropdown-trigger');
                const menu = dropdown.querySelector('.dropdown-menu');
                
                if (trigger && menu) {
                    trigger.addEventListener('click', function(e) {
                        e.preventDefault();
                        menu.classList.toggle('show');
                    });
                    
                    document.addEventListener('click', function(e) {
                        if (!dropdown.contains(e.target)) {
                            menu.classList.remove('show');
                        }
                    });
                }
            });
        }
    },
    
    init: function() {
        document.addEventListener('DOMContentLoaded', function() {
            KuaforApp.ui.initTooltips();
            KuaforApp.ui.initDropdowns();
            
            if (window.location.pathname.includes('/Appointments/Create') || 
                window.location.pathname.includes('/Appointments/Edit')) {
                KuaforApp.appointments.initAppointmentForm();
            }
            
            console.log('KuaforApp başlatıldı!');
        });
    }
};

KuaforApp.init();
