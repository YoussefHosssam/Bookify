// Enhanced form validation
document.addEventListener('DOMContentLoaded', function() {
    // Add validation styles to forms
    const forms = document.querySelectorAll('form[data-validate]');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
                
                // Show validation errors
                const firstInvalid = form.querySelector(':invalid');
                if (firstInvalid) {
                    firstInvalid.focus();
                    firstInvalid.classList.add('is-invalid');
                    
                    // Scroll to first error
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    
                    toastr.error('Please fill in all required fields correctly', 'Validation Error');
                }
            }
            
            form.classList.add('was-validated');
        });
    });
    
    // Real-time validation feedback
    const inputs = document.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        input.addEventListener('blur', function() {
            if (input.checkValidity()) {
                input.classList.remove('is-invalid');
                input.classList.add('is-valid');
            } else {
                input.classList.remove('is-valid');
                input.classList.add('is-invalid');
            }
        });
        
        input.addEventListener('input', function() {
            if (input.classList.contains('is-invalid') && input.checkValidity()) {
                input.classList.remove('is-invalid');
                input.classList.add('is-valid');
            }
        });
    });
});

// Date validation helper
function validateDates(checkInId, checkOutId) {
    const checkIn = document.getElementById(checkInId);
    const checkOut = document.getElementById(checkOutId);
    
    if (checkIn && checkOut) {
        checkIn.addEventListener('change', function() {
            const checkInDate = new Date(this.value);
            const tomorrow = new Date(checkInDate);
            tomorrow.setDate(tomorrow.getDate() + 1);
            
            checkOut.min = tomorrow.toISOString().split('T')[0];
            
            if (checkOut.value && new Date(checkOut.value) <= checkInDate) {
                checkOut.value = '';
                checkOut.classList.add('is-invalid');
            }
        });
        
        checkOut.addEventListener('change', function() {
            const checkInDate = new Date(checkIn.value);
            const checkOutDate = new Date(this.value);
            
            if (checkOutDate <= checkInDate) {
                this.classList.add('is-invalid');
                toastr.error('Check-out date must be after check-in date', 'Invalid Date');
            } else {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            }
        });
    }
}

