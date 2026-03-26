document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.getElementById('togglePw');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            const i = document.getElementById('pwInput');
            if (i) i.type = i.type === 'password' ? 'text' : 'password';
        });
    }
});
