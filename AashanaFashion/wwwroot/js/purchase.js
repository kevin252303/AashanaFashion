(function () {
    const itemsBody = document.getElementById('itemsBody');
    const addRowBtn = document.getElementById('addRow');
    const subtotalLabel = document.getElementById('subtotalLabel');
    const grandTotal = document.getElementById('grandTotal');
    const transportCharge = document.getElementById('transportCharge');
    const roundOff = document.getElementById('roundOff');

    function fmt(n) {
        return '\u20B9' + n.toFixed(2);
    }

    function recalcRow(row) {
        const qty = parseFloat(row.querySelector('.item-qty')?.value) || 0;
        const rate = parseFloat(row.querySelector('.item-rate')?.value) || 0;
        const discPct = parseFloat(row.querySelector('.item-disc')?.value) || 0;
        const gstPct = parseFloat(row.querySelector('.item-gst')?.value) || 0;

        const total = qty * rate;
        const discAmt = total * discPct / 100;
        const gstAmt = (total - discAmt) * gstPct / 100;
        const net = total - discAmt + gstAmt;

        const netEl = row.querySelector('.item-net');
        if (netEl) netEl.textContent = fmt(net);

        return net;
    }

    function recalcAll() {
        let subtotal = 0;
        document.querySelectorAll('.item-row').forEach(row => {
            subtotal += recalcRow(row);
        });

        const tc = parseFloat(transportCharge?.value) || 0;
        const ro = parseFloat(roundOff?.value) || 0;
        const total = subtotal + tc + ro;

        if (subtotalLabel) subtotalLabel.textContent = 'Subtotal: ' + fmt(subtotal);
        if (grandTotal) grandTotal.textContent = fmt(total);
    }

    function getRowCount() {
        return document.querySelectorAll('.item-row').length;
    }

    function createRow(index) {
        const tr = document.createElement('tr');
        tr.className = 'item-row';
        tr.innerHTML =
            '<td class="text-center sr-no">' + (index + 1) + '</td>' +
            '<td><input name="Details[' + index + '].ProductName" class="af-input" placeholder="Product" required /></td>' +
            '<td><input name="Details[' + index + '].ProductDesignNo" class="af-input" placeholder="Design #" /></td>' +
            '<td><input name="Details[' + index + '].HsnCode" class="af-input" placeholder="HSN" /></td>' +
            '<td><select name="Details[' + index + '].Unit" class="af-input af-select unit-select">' +
                '<option value="Piece" selected>Piece</option>' +
                '<option value="Meter">Meter</option>' +
            '</select></td>' +
            '<td><input name="Details[' + index + '].Quantity" class="af-input item-qty" type="number" min="1" value="1" required /></td>' +
            '<td><input name="Details[' + index + '].UnitPrice" class="af-input item-rate" type="number" step="0.01" min="0" value="0" required /></td>' +
            '<td><input name="Details[' + index + '].DiscountPercentage" class="af-input item-disc" type="number" step="0.01" min="0" max="100" value="0" /></td>' +
            '<td><input name="Details[' + index + '].GstPercentage" class="af-input item-gst" type="number" step="0.01" min="0" max="100" value="0" /></td>' +
            '<td><span class="item-net">\u20B90.00</span></td>' +
            '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-row">\u00D7</button></td>';
        return tr;
    }

    function reindex() {
        const rows = document.querySelectorAll('.item-row');
        rows.forEach((row, idx) => {
            row.querySelectorAll('input, select').forEach(el => {
                const name = el.getAttribute('name');
                if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
            });
            const sr = row.querySelector('.sr-no');
            if (sr) sr.textContent = idx + 1;
            const removeBtn = row.querySelector('.remove-row');
            if (removeBtn) removeBtn.style.display = rows.length > 1 ? '' : 'none';
        });
        const dc = document.getElementById('detailCount');
        if (dc) dc.value = rows.length;
        recalcAll();
    }

    function bindRow(row) {
        row.querySelector('.item-qty')?.addEventListener('input', recalcAll);
        row.querySelector('.item-rate')?.addEventListener('input', recalcAll);
        row.querySelector('.item-disc')?.addEventListener('input', recalcAll);
        row.querySelector('.item-gst')?.addEventListener('input', recalcAll);
        const removeBtn = row.querySelector('.remove-row');
        if (removeBtn) {
            removeBtn.addEventListener('click', function () {
                if (document.querySelectorAll('.item-row').length > 1) {
                    row.remove();
                    reindex();
                }
            });
        }
    }

    if (addRowBtn) {
        addRowBtn.addEventListener('click', function () {
            const count = getRowCount();
            const row = createRow(count);
            itemsBody.appendChild(row);
            bindRow(row);
            reindex();
        });
    }

    document.querySelectorAll('.item-row').forEach(bindRow);

    if (transportCharge) transportCharge.addEventListener('input', recalcAll);
    if (roundOff) roundOff.addEventListener('input', recalcAll);

    // Delay warning for ExpectedReceivingDate
    const erdInput = document.getElementById('expectedReceivingDate');
    const delayWarning = document.getElementById('delayWarning');
    if (erdInput && delayWarning) {
        function checkDelay() {
            if (!erdInput.value) { delayWarning.style.display = 'none'; return; }
            const erd = new Date(erdInput.value);
            const today = new Date();
            today.setHours(0,0,0,0);
            const diff = Math.floor((today - erd) / (1000 * 60 * 60 * 24));
            if (diff > 0) {
                delayWarning.style.display = 'block';
                delayWarning.style.background = '#fef2f2';
                delayWarning.style.color = '#dc2626';
                delayWarning.style.border = '1px solid #fecaca';
                delayWarning.textContent = '⚠ Overdue by ' + diff + ' day' + (diff > 1 ? 's' : '');
            } else {
                delayWarning.style.display = 'none';
            }
        }
        erdInput.addEventListener('change', checkDelay);
        checkDelay();
    }

    recalcAll();
})();
