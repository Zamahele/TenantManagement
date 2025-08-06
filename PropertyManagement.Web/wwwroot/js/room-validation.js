// Enhanced room form validation with detailed error messages
document.addEventListener('DOMContentLoaded', function() {
    const roomForm = document.getElementById('roomForm');
    if (roomForm) {
        
        // Add real-time validation feedback
        const inputs = roomForm.querySelectorAll('input, select');
        inputs.forEach(input => {
            input.addEventListener('blur', validateField);
            input.addEventListener('change', validateField);
        });

        function validateField(event) {
            const field = event.target;
            const fieldName = field.name;
            const value = field.value.trim();
            const errorSpan = field.parentNode.querySelector('.validation-error');
            
            // Clear previous validation styling
            field.classList.remove('is-invalid', 'is-valid');
            if (errorSpan) {
                errorSpan.textContent = '';
            }

            let isValid = true;
            let errorMessage = '';

            switch (fieldName) {
                case 'Number':
                    if (!value) {
                        isValid = false;
                        errorMessage = '? Room number is required - please enter a room number';
                    } else if (value.length > 10) {
                        isValid = false;
                        errorMessage = `? Room number is too long - maximum 10 characters allowed (current: ${value.length} characters)`;
                    } else {
                        // Check for valid format (optional - you can customize this)
                        if (!/^[A-Za-z0-9\-_]+$/.test(value)) {
                            isValid = false;
                            errorMessage = '? Room number can only contain letters, numbers, hyphens, and underscores';
                        }
                    }
                    break;

                case 'Type':
                    if (!value) {
                        isValid = false;
                        errorMessage = '? Room type is required - please select a room type';
                    }
                    break;

                case 'Status':
                    if (!value) {
                        isValid = false;
                        errorMessage = '? Room status is required - please select a status';
                    } else if (!['Available', 'Occupied', 'Under Maintenance'].includes(value)) {
                        isValid = false;
                        errorMessage = `? Invalid status selected - must be 'Available', 'Occupied', or 'Under Maintenance' (current: '${value}')`;
                    }
                    break;
            }

            // Apply validation styling and message
            if (isValid) {
                field.classList.add('is-valid');
                if (errorSpan) {
                    errorSpan.textContent = '';
                }
            } else {
                field.classList.add('is-invalid');
                if (errorSpan) {
                    errorSpan.textContent = errorMessage;
                }
            }
        }

        // Form submission validation
        roomForm.addEventListener('submit', function(event) {
            let isFormValid = true;
            const formData = new FormData(roomForm);
            
            // Validate all fields before submission
            inputs.forEach(input => {
                const validationEvent = new Event('blur');
                input.dispatchEvent(validationEvent);
                
                if (input.classList.contains('is-invalid')) {
                    isFormValid = false;
                }
            });

            if (!isFormValid) {
                event.preventDefault();
                
                // Show summary message
                showValidationSummary();
                
                // Focus on first invalid field
                const firstInvalid = roomForm.querySelector('.is-invalid');
                if (firstInvalid) {
                    firstInvalid.focus();
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        });

        function showValidationSummary() {
            // Remove existing summary
            const existingSummary = roomForm.querySelector('.validation-summary');
            if (existingSummary) {
                existingSummary.remove();
            }

            // Create validation summary
            const summary = document.createElement('div');
            summary.className = 'alert alert-danger validation-summary mt-3';
            summary.innerHTML = `
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <strong>Please fix the following errors before saving:</strong>
            `;

            // Add to top of form
            roomForm.insertBefore(summary, roomForm.firstChild);

            // Auto-hide after 5 seconds
            setTimeout(() => {
                if (summary.parentNode) {
                    summary.remove();
                }
            }, 5000);
        }
    }
});