/* ==========================================
   LibraryPro Single Card 3D Flip Interactions
   ========================================== */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Password Visibility Toggle
    const toggleBtns = document.querySelectorAll('.toggle-pass-btn');
    toggleBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');
            
            if (input && icon) {
                if (input.type === 'password') {
                    input.type = 'text';
                    icon.classList.remove('bi-eye');
                    icon.classList.add('bi-eye-slash');
                } else {
                    input.type = 'password';
                    icon.classList.remove('bi-eye-slash');
                    icon.classList.add('bi-eye');
                }
            }
        });
    });

    // 2. Single 3D Card Flip Handler
    const flipInner = document.getElementById('authFlipInner');
    const flipToRegisterBtns = document.querySelectorAll('.trigger-flip-register');
    const flipToLoginBtns = document.querySelectorAll('.trigger-flip-login');

    if (flipInner) {
        flipToRegisterBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                flipInner.classList.add('flipped');
                if (window.history.pushState) {
                    history.pushState(null, '', '/Account/Register');
                }
                document.title = 'Create Account - LibraryPro';
            });
        });

        flipToLoginBtns.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                flipInner.classList.remove('flipped');
                if (window.history.pushState) {
                    history.pushState(null, '', '/Account/Login');
                }
                document.title = 'Sign In - LibraryPro';
            });
        });

        // Initialize flip orientation based on current pathname
        if (window.location.pathname.toLowerCase().includes('/account/register')) {
            flipInner.classList.add('flipped');
        } else {
            flipInner.classList.remove('flipped');
        }
    }

    // 3. Quick Fill Demo Credentials Helper
    const demoPills = document.querySelectorAll('.demo-btn-pill');
    demoPills.forEach(pill => {
        pill.addEventListener('click', function () {
            const email = this.getAttribute('data-email');
            const pass = this.getAttribute('data-pass');
            
            const emailInput = document.querySelector('#loginForm #Email');
            const passInput = document.querySelector('#loginForm #Password');
            
            if (emailInput && passInput) {
                emailInput.value = email;
                passInput.value = pass;
                
                emailInput.focus();
                setTimeout(() => passInput.focus(), 150);
            }
        });
    });

    // 4. Form Submit Loading Spinner
    const authForms = document.querySelectorAll('.auth-submit-form');
    authForms.forEach(form => {
        form.addEventListener('submit', function () {
            const submitBtn = form.querySelector('.btn-submit-light');
            if (submitBtn) {
                const btnText = submitBtn.querySelector('.btn-text');
                const btnSpinner = submitBtn.querySelector('.spinner-border');
                const btnIcon = submitBtn.querySelector('.bi-arrow-right, .bi-check-circle');
                
                if (btnText) btnText.textContent = 'Processing...';
                if (btnSpinner) btnSpinner.classList.remove('d-none');
                if (btnIcon) btnIcon.classList.add('d-none');
                
                submitBtn.disabled = true;
            }
        });
    });
});
