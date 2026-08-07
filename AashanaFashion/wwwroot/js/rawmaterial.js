(function () {
    var entriesBody = document.getElementById('entriesBody');
    var addRowBtn = document.getElementById('addRow');
    var materialSelect = document.getElementById('materialSelect');
    var currentStockDisplay = document.getElementById('currentStockDisplay');
    var minStockDisplay = document.getElementById('minStockDisplay');
    var entryCount = document.getElementById('entryCount');

    // Stock data loaded from the server for each material
    var stockData = {};

    function getRowCount() {
        return document.querySelectorAll('.entry-row').length;
    }

    function createRow(index) {
        var tr = document.createElement('tr');
        tr.className = 'entry-row';
        tr.innerHTML =
            '<td class="text-center sr-no">' + (index + 1) + '</td>' +
            '<td>' +
                '<select name="Lines[' + index + '].Type" class="af-input af-select entry-type">' +
                    '<option value="Inward">Inward</option>' +
                    '<option value="Outward">Outward</option>' +
                '</select>' +
            '</td>' +
            '<td><input name="Lines[' + index + '].Quantity" class="af-input entry-qty" type="number" step="0.01" min="0" value="" required /></td>' +
            '<td><input name="Lines[' + index + '].Remarks" class="af-input" placeholder="Optional" /></td>' +
            '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-row">\u00D7</button></td>';
        return tr;
    }

    function reindex() {
        var rows = document.querySelectorAll('.entry-row');
        rows.forEach(function (row, idx) {
            row.querySelectorAll('input, select').forEach(function (el) {
                var name = el.getAttribute('name');
                if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
            });
            var sr = row.querySelector('.sr-no');
            if (sr) sr.textContent = idx + 1;
            var removeBtn = row.querySelector('.remove-row');
            if (removeBtn) removeBtn.style.display = rows.length > 1 ? '' : 'none';
        });
        if (entryCount) entryCount.value = rows.length;
    }

    function bindRow(row) {
        var removeBtn = row.querySelector('.remove-row');
        if (removeBtn) {
            removeBtn.addEventListener('click', function () {
                if (document.querySelectorAll('.entry-row').length > 1) {
                    row.remove();
                    reindex();
                }
            });
        }
    }

    if (addRowBtn) {
        addRowBtn.addEventListener('click', function () {
            var count = getRowCount();
            var row = createRow(count);
            entriesBody.appendChild(row);
            bindRow(row);
            reindex();
        });
    }

    document.querySelectorAll('.entry-row').forEach(bindRow);

    // Material change handler
    if (materialSelect) {
        materialSelect.addEventListener('change', function () {
            var id = this.value;
            if (id) {
                window.location.href = window.location.pathname + '?materialId=' + id;
            } else {
                currentStockDisplay.value = '';
                minStockDisplay.value = '';
            }
        });
    }
})();
