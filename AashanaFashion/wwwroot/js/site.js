// App shell behaviour — sidebar, collapsible nav groups, dismissable alerts and toasts.
(function () {
    'use strict';

    var sidebar = document.getElementById('sidebar');

    function setSidebar(open) {
        if (!sidebar) return;
        sidebar.classList.toggle('open', open);
        document.body.style.overflow = open ? 'hidden' : '';
    }

    document.addEventListener('click', function (e) {
        // Mobile drawer
        if (e.target.closest('[data-sidebar-toggle]')) {
            setSidebar(!(sidebar && sidebar.classList.contains('open')));
            return;
        }
        if (e.target.closest('[data-sidebar-close]')) {
            setSidebar(false);
            return;
        }

        // Collapsible sidebar groups
        var toggle = e.target.closest('[data-sb-toggle]');
        if (toggle) {
            var group = toggle.parentElement;
            var open = !group.classList.contains('open');
            group.classList.toggle('open', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            return;
        }

        // Inline alert / toast dismiss
        var close = e.target.closest('.alert-close');
        if (close) {
            var box = close.closest('.af-toast, .af-alert, .alert');
            if (box) dismiss(box);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar && sidebar.classList.contains('open')) setSidebar(false);
    });

    function dismiss(el) {
        if (el.classList.contains('af-toast')) {
            el.classList.add('hiding');
            setTimeout(function () { el.remove(); }, 180);
        } else {
            el.remove();
        }
    }

    // Confirmation toasts hide themselves
    document.querySelectorAll('.af-toast[data-autohide]').forEach(function (t, i) {
        setTimeout(function () { if (t.isConnected) dismiss(t); }, 5000 + i * 600);
    });

    // Keep the active sidebar entry in view on long menus
    var active = document.querySelector('.sidebar .sb-sublink.active, .sidebar .sb-link.active');
    if (active && active.scrollIntoView) active.scrollIntoView({ block: 'nearest' });

    // Public helper so page scripts can raise the same toast UI
    window.afToast = function (message, type) {
        type = type || 'success';
        var icons = { success: 'bi-check-circle-fill', error: 'bi-exclamation-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
        var stack = document.querySelector('.toast-stack');
        if (!stack) {
            stack = document.createElement('div');
            stack.className = 'toast-stack';
            document.body.appendChild(stack);
        }
        var t = document.createElement('div');
        t.className = 'af-toast ' + type;
        t.setAttribute('role', 'status');
        var icon = document.createElement('i');
        icon.className = 'bi ' + (icons[type] || icons.info);
        var body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = message;
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'alert-close';
        btn.setAttribute('aria-label', 'Dismiss');
        btn.innerHTML = '&times;';
        t.append(icon, body, btn);
        stack.appendChild(t);
        setTimeout(function () { if (t.isConnected) dismiss(t); }, 5000);
        return t;
    };
})();
