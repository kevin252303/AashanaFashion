document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.getElementById('togglePw');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            const i = document.getElementById('pwInput');
            if (!i) return;
            const show = i.type === 'password';
            i.type = show ? 'text' : 'password';
            toggleBtn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            const icon = toggleBtn.querySelector('.bi');
            if (icon) icon.className = 'bi ' + (show ? 'bi-eye-slash' : 'bi-eye');
        });
    }
});
