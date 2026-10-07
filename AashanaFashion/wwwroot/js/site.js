// App shell behaviour — sidebar, collapsible nav groups, dismissable alerts and toasts.
(function () {
    'use strict';

    var sidebar = document.getElementById('sidebar');

    function isDesktop() {
        return window.innerWidth >= 992;
    }

    function initSidebarState() {
        if (!sidebar) return;
        if (isDesktop()) {
            var isPinned = localStorage.getItem('sidebar_pinned') === 'true';
            document.body.classList.toggle('sidebar-pinned', isPinned);
        } else {
            document.body.classList.remove('sidebar-pinned');
        }

        // Add title to links for quick tooltip in collapsed rail mode
        document.querySelectorAll('.sb-link').forEach(function (link) {
            if (!link.getAttribute('title')) {
                var span = link.querySelector('span');
                if (span && span.textContent.trim()) {
                    link.setAttribute('title', span.textContent.trim());
                }
            }
        });
    }

    function setSidebar(open) {
        if (!sidebar) return;
        sidebar.classList.toggle('open', open);
        document.body.style.overflow = open ? 'hidden' : '';
    }

    // Initialize on load and resize
    initSidebarState();
    window.addEventListener('resize', function () {
        if (!isDesktop() && document.body.classList.contains('sidebar-pinned')) {
            document.body.classList.remove('sidebar-pinned');
        } else if (isDesktop()) {
            var isPinned = localStorage.getItem('sidebar_pinned') === 'true';
            document.body.classList.toggle('sidebar-pinned', isPinned);
        }
    });

    document.addEventListener('click', function (e) {
        // Toggle (desktop pin/unpin or mobile drawer)
        var toggleBtn = e.target.closest('[data-sidebar-toggle]');
        if (toggleBtn) {
            if (isDesktop()) {
                var pinned = !document.body.classList.contains('sidebar-pinned');
                document.body.classList.toggle('sidebar-pinned', pinned);
                localStorage.setItem('sidebar_pinned', pinned ? 'true' : 'false');
            } else {
                setSidebar(!(sidebar && sidebar.classList.contains('open')));
            }
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

    // ── UNIVERSAL TABLE CUSTOM FILTER BUILDER ────────────────────────────
    function initCustomFilters() {
        var tableCards = document.querySelectorAll('.table-card');

        tableCards.forEach(function (card) {
            var table = card.querySelector('table.af-table, table.table');
            if (!table || table.classList.contains('table-edit')) return;

            var thead = table.querySelector('thead');
            var tbody = table.querySelector('tbody');
            if (!thead || !tbody) return;

            // Detect filterable columns
            var thList = thead.querySelectorAll('tr:last-child th');
            var columns = [];
            thList.forEach(function (th, idx) {
                var text = (th.textContent || '').trim();
                var lower = text.toLowerCase();
                if (!text || lower === 'actions' || lower === 'action' || lower === 'select' || lower === '') return;
                columns.push({ index: idx, name: text });
            });

            if (columns.length === 0) return;

            // Ensure toolbar exists
            var toolbar = card.querySelector('.table-toolbar');
            var toolbarActions = toolbar ? toolbar.querySelector('.toolbar-actions') : null;

            if (!toolbar) {
                toolbar = document.createElement('div');
                toolbar.className = 'table-toolbar';
                var filtersDiv = document.createElement('div');
                filtersDiv.className = 'toolbar-filters';
                toolbarActions = document.createElement('div');
                toolbarActions.className = 'toolbar-actions';
                toolbar.appendChild(filtersDiv);
                toolbar.appendChild(toolbarActions);
                var scrollEl = card.querySelector('.table-scroll') || table;
                card.insertBefore(toolbar, scrollEl);
            } else if (!toolbarActions) {
                toolbarActions = document.createElement('div');
                toolbarActions.className = 'toolbar-actions';
                toolbar.appendChild(toolbarActions);
            }

            // Create "Custom filter" trigger button if not already created
            if (toolbarActions.querySelector('.custom-filter-toggle-btn')) return;

            var toggleBtn = document.createElement('button');
            toggleBtn.type = 'button';
            toggleBtn.className = 'btn btn-secondary btn-sm custom-filter-toggle-btn';
            toggleBtn.innerHTML = '<i class="bi bi-funnel"></i> Custom filter <span class="count-pill d-none" style="margin-left:4px;">0</span>';
            toolbarActions.appendChild(toggleBtn);

            // Create Drawer Container
            var drawer = document.createElement('div');
            drawer.className = 'custom-filter-container';
            drawer.innerHTML = [
                '<div class="d-flex justify-content-between align-items-center mb-2">',
                '  <div class="text-xs text-muted text-uppercase fw-bold"><i class="bi bi-sliders me-1"></i> Custom Filter Rules</div>',
                '  <button type="button" class="btn-close btn-close-sm custom-filter-close-btn" aria-label="Close"></button>',
                '</div>',
                '<div class="filter-rule-list"></div>',
                '<div class="d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2">',
                '  <button type="button" class="btn btn-ghost btn-sm add-rule-btn"><i class="bi bi-plus-lg"></i> Add rule</button>',
                '  <div class="d-flex gap-2">',
                '    <button type="button" class="btn btn-ghost btn-sm reset-filter-btn">Reset</button>',
                '    <button type="button" class="btn btn-primary btn-sm apply-filter-btn"><i class="bi bi-check2"></i> Apply filter</button>',
                '  </div>',
                '</div>'
            ].join('');

            // Create Active Chips Bar
            var chipsBar = document.createElement('div');
            chipsBar.className = 'filter-chips-bar d-none';

            // Insert drawer & chips bar right above table-scroll
            var tableScroll = card.querySelector('.table-scroll') || table;
            card.insertBefore(drawer, tableScroll);
            card.insertBefore(chipsBar, tableScroll);

            var ruleList = drawer.querySelector('.filter-rule-list');
            var countPill = toggleBtn.querySelector('.count-pill');
            var activeRules = [];
            var originalFooterText = null;
            var footerSpan = card.querySelector('.table-footer span');
            if (footerSpan) originalFooterText = footerSpan.innerHTML;

            function createRuleRow(colIdx, op, val) {
                var row = document.createElement('div');
                row.className = 'filter-rule-row';

                var colSelect = document.createElement('select');
                colSelect.className = 'af-input af-input-sm af-select filter-rule-col-select';
                columns.forEach(function (c) {
                    var opt = document.createElement('option');
                    opt.value = c.index;
                    opt.textContent = c.name;
                    if (colIdx !== undefined && c.index === parseInt(colIdx, 10)) opt.selected = true;
                    colSelect.appendChild(opt);
                });

                var opSelect = document.createElement('select');
                opSelect.className = 'af-input af-input-sm af-select filter-rule-op-select';
                var ops = [
                    { val: 'contains', text: 'contains' },
                    { val: 'not_contains', text: "doesn't contain" },
                    { val: 'equals', text: 'is equal to' },
                    { val: 'not_equals', text: 'is not equal to' },
                    { val: 'starts_with', text: 'starts with' },
                    { val: 'gt', text: 'greater than (>)' },
                    { val: 'lt', text: 'less than (<)' },
                    { val: 'is_empty', text: 'is empty / blank' },
                    { val: 'is_not_empty', text: 'is not empty' }
                ];
                ops.forEach(function (o) {
                    var opt = document.createElement('option');
                    opt.value = o.val;
                    opt.textContent = o.text;
                    if (op && o.val === op) opt.selected = true;
                    opSelect.appendChild(opt);
                });

                var valInput = document.createElement('input');
                valInput.type = 'text';
                valInput.className = 'af-input af-input-sm filter-rule-val-input';
                valInput.placeholder = 'Enter filter value…';
                if (val !== undefined) valInput.value = val;

                opSelect.addEventListener('change', function () {
                    var isBlankOp = opSelect.value === 'is_empty' || opSelect.value === 'is_not_empty';
                    valInput.style.display = isBlankOp ? 'none' : '';
                });

                var removeBtn = document.createElement('button');
                removeBtn.type = 'button';
                removeBtn.className = 'row-action danger';
                removeBtn.title = 'Remove condition';
                removeBtn.innerHTML = '<i class="bi bi-trash"></i>';
                removeBtn.addEventListener('click', function () {
                    row.remove();
                    if (ruleList.children.length === 0) addRule();
                });

                row.appendChild(colSelect);
                row.appendChild(opSelect);
                row.appendChild(valInput);
                row.appendChild(removeBtn);
                ruleList.appendChild(row);
            }

            function addRule() {
                createRuleRow();
            }

            // Initially add 1 rule
            addRule();

            // Toggle drawer
            toggleBtn.addEventListener('click', function () {
                var isOpen = drawer.classList.contains('is-open');
                drawer.classList.toggle('is-open', !isOpen);
                toggleBtn.classList.toggle('active', !isOpen);
            });

            drawer.querySelector('.custom-filter-close-btn').addEventListener('click', function () {
                drawer.classList.remove('is-open');
                toggleBtn.classList.remove('active');
            });

            drawer.querySelector('.add-rule-btn').addEventListener('click', addRule);

            drawer.querySelector('.reset-filter-btn').addEventListener('click', function () {
                ruleList.innerHTML = '';
                addRule();
                applyFilterRules([]);
            });

            drawer.querySelector('.apply-filter-btn').addEventListener('click', function () {
                var rules = [];
                var rows = ruleList.querySelectorAll('.filter-rule-row');
                rows.forEach(function (r) {
                    var colIdx = parseInt(r.querySelector('.filter-rule-col-select').value, 10);
                    var op = r.querySelector('.filter-rule-op-select').value;
                    var val = (r.querySelector('.filter-rule-val-input').value || '').trim();
                    var colName = columns.find(function (c) { return c.index === colIdx; })?.name || ('Column ' + colIdx);

                    if (op !== 'is_empty' && op !== 'is_not_empty' && !val) return;

                    rules.push({ colIndex: colIdx, colName: colName, op: op, val: val });
                });

                applyFilterRules(rules);
                drawer.classList.remove('is-open');
                toggleBtn.classList.remove('active');
            });

            function cleanNumber(str) {
                var cleaned = (str || '').replace(/[₹,\s]/g, '');
                var num = parseFloat(cleaned);
                return isNaN(num) ? null : num;
            }

            function rowMatchesRule(cellText, op, val) {
                var cText = (cellText || '').trim().toLowerCase();
                var vText = (val || '').trim().toLowerCase();

                if (op === 'contains') return cText.indexOf(vText) !== -1;
                if (op === 'not_contains') return cText.indexOf(vText) === -1;
                if (op === 'equals') return cText === vText;
                if (op === 'not_equals') return cText !== vText;
                if (op === 'starts_with') return cText.indexOf(vText) === 0;
                if (op === 'is_empty') return cText === '' || cText === '—' || cText === '-';
                if (op === 'is_not_empty') return cText !== '' && cText !== '—' && cText !== '-';

                if (op === 'gt' || op === 'lt') {
                    var cNum = cleanNumber(cellText);
                    var vNum = cleanNumber(val);
                    if (cNum !== null && vNum !== null) {
                        return op === 'gt' ? cNum > vNum : cNum < vNum;
                    }
                    // Fallback to date comparison
                    var cDate = Date.parse(cellText);
                    var vDate = Date.parse(val);
                    if (!isNaN(cDate) && !isNaN(vDate)) {
                        return op === 'gt' ? cDate > vDate : cDate < vDate;
                    }
                }

                return cText.indexOf(vText) !== -1;
            }

            function applyFilterRules(rules) {
                activeRules = rules;

                // Update count pill
                if (rules.length > 0) {
                    countPill.textContent = rules.length;
                    countPill.classList.remove('d-none');
                    toggleBtn.classList.add('btn-primary');
                    toggleBtn.classList.remove('btn-secondary');
                } else {
                    countPill.classList.add('d-none');
                    toggleBtn.classList.remove('btn-primary');
                    toggleBtn.classList.add('btn-secondary');
                }

                // Render active chips
                chipsBar.innerHTML = '';
                if (rules.length > 0) {
                    chipsBar.classList.remove('d-none');
                    var label = document.createElement('span');
                    label.className = 'text-xs text-muted fw-semibold me-1';
                    label.innerHTML = '<i class="bi bi-funnel"></i> Filters:';
                    chipsBar.appendChild(label);

                    rules.forEach(function (rule, rIdx) {
                        var chip = document.createElement('span');
                        chip.className = 'filter-chip';
                        var opLabel = rule.op.replace(/_/g, ' ');
                        chip.innerHTML = '<strong>' + rule.colName + '</strong> ' + opLabel + (rule.val ? ' "' + rule.val + '"' : '') +
                            ' <button type="button" class="filter-chip-remove" aria-label="Remove filter">&times;</button>';

                        chip.querySelector('.filter-chip-remove').addEventListener('click', function () {
                            rules.splice(rIdx, 1);
                            applyFilterRules(rules);
                        });

                        chipsBar.appendChild(chip);
                    });

                    var clearAllBtn = document.createElement('button');
                    clearAllBtn.type = 'button';
                    clearAllBtn.className = 'btn btn-ghost btn-sm py-0 text-xs ms-2';
                    clearAllBtn.textContent = 'Clear all';
                    clearAllBtn.addEventListener('click', function () {
                        applyFilterRules([]);
                    });
                    chipsBar.appendChild(clearAllBtn);
                } else {
                    chipsBar.classList.add('d-none');
                }

                // Filter table rows
                var rows = tbody.querySelectorAll('tr:not(.table-filter-empty-row)');
                var visibleCount = 0;
                var totalCount = rows.length;

                rows.forEach(function (row) {
                    var matches = true;
                    for (var i = 0; i < rules.length; i++) {
                        var rule = rules[i];
                        var cell = row.children[rule.colIndex];
                        var cellText = cell ? cell.textContent : '';
                        if (!rowMatchesRule(cellText, rule.op, rule.val)) {
                            matches = false;
                            break;
                        }
                    }

                    if (matches) {
                        row.style.display = '';
                        visibleCount++;
                    } else {
                        row.style.display = 'none';
                    }
                });

                // In-table empty state
                var emptyRow = tbody.querySelector('.table-filter-empty-row');
                if (visibleCount === 0 && rules.length > 0 && totalCount > 0) {
                    if (!emptyRow) {
                        emptyRow = document.createElement('tr');
                        emptyRow.className = 'table-filter-empty-row';
                        var td = document.createElement('td');
                        td.colSpan = thList.length || 10;
                        td.innerHTML = '<i class="bi bi-funnel text-muted fs-4 d-block mb-1"></i> No matching rows found for custom filter. <button type="button" class="btn btn-secondary btn-sm ms-2 clear-custom-filter">Clear filter</button>';
                        emptyRow.appendChild(td);
                        tbody.appendChild(emptyRow);

                        emptyRow.querySelector('.clear-custom-filter').addEventListener('click', function () {
                            applyFilterRules([]);
                        });
                    } else {
                        emptyRow.style.display = '';
                    }
                } else if (emptyRow) {
                    emptyRow.style.display = 'none';
                }

                // Update footer count
                if (footerSpan && originalFooterText) {
                    if (rules.length > 0) {
                        footerSpan.innerHTML = 'Showing <strong>' + visibleCount + '</strong> of ' + totalCount + ' items (filtered by ' + rules.length + ' custom rule' + (rules.length > 1 ? 's' : '') + ')';
                    } else {
                        footerSpan.innerHTML = originalFooterText;
                    }
                }
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCustomFilters);
    } else {
        initCustomFilters();
    }
})();

