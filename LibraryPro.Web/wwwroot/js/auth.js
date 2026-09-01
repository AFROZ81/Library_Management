document.addEventListener('DOMContentLoaded', function () {
    const toggleBtns = document.querySelectorAll('.toggle-pass-btn');

    toggleBtns.forEach((btn) => {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');

            if (!input || !icon) return;

            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.remove('bi-eye');
                icon.classList.add('bi-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.remove('bi-eye-slash');
                icon.classList.add('bi-eye');
            }
        });
    });

    const cardRotator = document.getElementById('authCardRotator');
    const switchLinks = document.querySelectorAll('.auth-switch-link');

    switchLinks.forEach((link) => {
        link.addEventListener('click', function (event) {
            event.preventDefault();

            if (!cardRotator) return;
            cardRotator.classList.toggle('is-flipped');
        });
    });

    const authForms = document.querySelectorAll('.auth-submit-form');
    authForms.forEach((form) => {
        form.addEventListener('submit', function () {
            const submitBtn = form.querySelector('.btn-submit-light');
            if (!submitBtn) return;

            const btnText = submitBtn.querySelector('.btn-text');
            const btnSpinner = submitBtn.querySelector('.spinner-border');

            if (btnText) btnText.textContent = 'Processing...';
            if (btnSpinner) btnSpinner.classList.remove('d-none');

            submitBtn.disabled = true;
        });
    });
});
