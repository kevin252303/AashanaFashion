$(document).ready(function () {
    let currentColours = [];
    let currentSizes = [];
    let currentDiscontinuedVariants = [];
    let lotComponentsList = ['Chaniya', 'Choli', 'Dupatta'];
    let lotMatrixState = {}; // key: comp + '___' + proc -> { vendorId, rate }
    let currentLotProcesses = [];

    $('#designSelect').change(function () {
        var selected = $(this).find(':selected');
        var colours = selected.data('colours') || '';
        var sizes = selected.data('sizes') || '';
        var steps = selected.data('steps') || '';
        var discRaw = selected.attr('data-discontinued-variants') || '[]';
        try {
            currentDiscontinuedVariants = JSON.parse(discRaw);
        } catch (e) {
            currentDiscontinuedVariants = [];
        }

        currentColours = colours.split(',').map(c => c.trim()).filter(c => c);
        currentSizes = sizes.split(',').map(s => s.trim()).filter(s => s);

        generateMatrix();

        // Populate components & process vendor planning matrix
        var compRaw = selected.attr('data-components') || '[]';
        var assignRaw = selected.attr('data-assignments') || '[]';
        try {
            lotComponentsList = JSON.parse(compRaw);
        } catch (e) {
            lotComponentsList = ['Chaniya', 'Choli', 'Dupatta'];
        }
        if (!lotComponentsList || !lotComponentsList.length) {
            lotComponentsList = ['Chaniya', 'Choli', 'Dupatta'];
        }

        lotMatrixState = {};
        try {
            var defaultAssignments = JSON.parse(assignRaw);
            if (defaultAssignments && defaultAssignments.length > 0) {
                defaultAssignments.forEach(function(a) {
                    lotMatrixState[a.componentName + '___' + a.processName] = { vendorId: a.vendorId, rate: a.rate };
                });
            }
        } catch (e) {
            lotMatrixState = {};
        }

        // Parse creation steps
        var stepNames = [];
        if (steps) {
            stepNames = steps.split(/[→,]/).map(s => s.trim()).filter(s => s);
        }
        if (!stepNames.length && typeof allMasterProcesses !== 'undefined' && allMasterProcesses.length > 0) {
            stepNames = allMasterProcesses.map(p => p.name);
        }
        currentLotProcesses = stepNames;

        renderLotComponentBadges();
        renderLotMatrixTable();
    });

    function isVariantDiscontinued(colour, size) {
        if (!currentDiscontinuedVariants || !currentDiscontinuedVariants.length) return false;
        var col = (colour || '').trim().toLowerCase();
        var sz = (size || '').trim().toLowerCase();
        return currentDiscontinuedVariants.some(function(v) {
            var vCol = (v.colour || '').trim().toLowerCase();
            var vSz = (v.size || '').trim().toLowerCase();
            if (vCol && vSz) return vCol === col && vSz === sz;
            if (vCol && !vSz) return vCol === col;
            if (!vCol && vSz) return vSz === sz;
            return false;
        });
    }

    function generateMatrix() {
        var tbody = $('#matrixBody');
        var theadRow = $('#matrixHeader');
        tbody.empty();

        if (currentColours.length === 0 || currentSizes.length === 0) {
            $('#matrixPlaceholder').show();
            $('#matrixGrid').hide();
            return;
        }

        $('#matrixPlaceholder').hide();
        $('#matrixGrid').show();

        // Clear header except first cell
        theadRow.find('th:not(:first)').remove();

        // Add size headers
        currentSizes.forEach(function (size) {
            theadRow.append('<th class="text-center" style="min-width:80px;">' + size + '</th>');
        });
        theadRow.append('<th class="text-center" style="min-width:80px;">Total</th>');

        // Generate matrix rows
        currentColours.forEach(function (colour) {
            var row = $('<tr>');
            row.append('<td class="cell-title">' + colour + '</td>');

            var rowIndex = currentColours.indexOf(colour);

            currentSizes.forEach(function (size, sizeIdx) {
                var detailIndex = rowIndex * currentSizes.length + sizeIdx;
                var isDisc = isVariantDiscontinued(colour, size);
                if (isDisc) {
                    row.append(
                        '<td class="matrix-disc" title="Variant is discontinued - cannot create production order">' +
                        '<span class="af-badge b-red">Disc.</span>' +
                        '<input type="hidden" name="Details[' + detailIndex + '].Colour" value="' + colour + '" />' +
                        '<input type="hidden" name="Details[' + detailIndex + '].Size" value="' + size + '" />' +
                        '<input type="hidden" name="Details[' + detailIndex + '].Quantity" value="0" />' +
                        '</td>'
                    );
                } else {
                    row.append(
                        '<td>' +
                        '<input type="hidden" name="Details[' + detailIndex + '].Colour" value="' + colour + '" />' +
                        '<input type="hidden" name="Details[' + detailIndex + '].Size" value="' + size + '" />' +
                        '<input type="number" class="af-input af-input-sm matrix-input" data-row="' + rowIndex + '" data-col="' + sizeIdx + '" ' +
                        'name="Details[' + detailIndex + '].Quantity" ' +
                        'min="0" value="0" />' +
                        '</td>'
                    );
                }
            });

            // Row total
            row.append('<td class="cell-total"><span class="row-total" data-row="' + rowIndex + '">0</span></td>');
            tbody.append(row);
        });

        // Add column totals row
        var totalRow = $('<tr class="matrix-total-row">');
        totalRow.append('<td>Total</td>');
        currentSizes.forEach(function (size, sizeIdx) {
            totalRow.append('<td><span class="col-total" data-col="' + sizeIdx + '">0</span></td>');
        });
        totalRow.append('<td class="grand-total"><span id="grandTotal">0</span></td>');
        tbody.append(totalRow);

        // Add event listeners
        $('.matrix-input').on('input', function () {
            updateTotals();
        });
    }

    function updateTotals() {
        var grandTotal = 0;

        // Row totals
        currentColours.forEach(function (colour, rowIdx) {
            var rowTotal = 0;
            $('.matrix-input[data-row="' + rowIdx + '"]').each(function () {
                rowTotal += parseInt($(this).val()) || 0;
            });
            $('.row-total[data-row="' + rowIdx + '"]').text(rowTotal);
            grandTotal += rowTotal;
        });

        // Column totals
        currentSizes.forEach(function (size, colIdx) {
            var colTotal = 0;
            $('.matrix-input[data-col="' + colIdx + '"]').each(function () {
                colTotal += parseInt($(this).val()) || 0;
            });
            $('.col-total[data-col="' + colIdx + '"]').text(colTotal);
        });

        $('#grandTotal').text(grandTotal);
        $('#totalPieces').text('Total: ' + grandTotal + ' pcs');
        $('#totalQuantity').val(grandTotal);
    }

    function updateVerificationSteps(steps) {
        var container = $('#verificationContainer');
        var verificationMap = {
            'Raw Material': 'IsRawMaterialVerified',
            'Dying': 'IsDyingVerified',
            'Handwork': 'IsHandworkVerified',
            'Stitching': 'IsStitchingVerified'
        };

        container.empty();

        if (!steps) {
            $('#verificationSection').hide();
            return;
        }

        $('#verificationSection').show();
        var stepsList = steps.split(',').map(s => s.trim()).filter(s => s);

        stepsList.forEach(function (step) {
            var fieldName = verificationMap[step];
            if (fieldName) {
                container.append(
                    '<label class="check-item">' +
                    '<input type="checkbox" class="form-check-input m-0" name="' + fieldName + '" />' +
                    step + ' Verified' +
                    '</label>'
                );
            }
        });
    }

    // ===== LOT COMPONENT & PROCESS VENDOR MATRIX FUNCTIONS =====
    function renderLotComponentBadges() {
        var html = '';
        lotComponentsList.forEach(function(comp) {
            html += `
                <span class="tag">
                    ${comp}
                    <button type="button" class="tag-remove remove-lot-component-btn" data-name="${comp}" title="Remove component" aria-label="Remove ${comp}"><i class="bi bi-x"></i></button>
                </span>
            `;
        });
        $('#lotComponentBadgesContainer').html(html);
        $('#lotComponentsHiddenInput').val(lotComponentsList.join(', '));
        $('#lotComponentCountBadge').text(lotComponentsList.length + (lotComponentsList.length === 1 ? ' Component' : ' Components'));
    }

    function renderLotMatrixTable() {
        var procs = currentLotProcesses || [];
        var thead = '<th style="min-width:140px;">Component</th>';
        procs.forEach(function(p) {
            thead += `<th style="min-width:180px;">${p}</th>`;
        });
        thead += '<th style="width:50px;"></th>';
        $('#lotMatrixHeaderRow').html(thead);

        var tbody = '';
        var assignmentIdx = 0;

        lotComponentsList.forEach(function(comp) {
            tbody += `<tr>
                <td class="cell-title text-nowrap">${comp}</td>`;

            procs.forEach(function(p) {
                var key = comp + '___' + p;
                var val = lotMatrixState[key] || { vendorId: '', rate: '' };

                var options = '<option value="">— In-house / None —</option>';
                if (typeof allVendors !== 'undefined') {
                    allVendors.forEach(function(v) {
                        var sel = (val.vendorId && String(val.vendorId) === String(v.id)) ? 'selected' : '';
                        options += `<option value="${v.id}" ${sel}>${v.name}</option>`;
                    });
                }

                tbody += `
                    <td>
                        <select name="ComponentAssignments[${assignmentIdx}].VendorId" class="form-select form-select-sm lot-matrix-vendor-select" data-comp="${comp}" data-proc="${p}">
                            ${options}
                        </select>
                        <div class="input-group input-group-sm mt-1">
                            <span class="input-group-text">₹</span>
                            <input type="number" step="0.01" min="0" name="ComponentAssignments[${assignmentIdx}].Rate" value="${val.rate || ''}" placeholder="Rate" class="form-control form-control-sm lot-matrix-rate-input" data-comp="${comp}" data-proc="${p}" />
                        </div>
                        <input type="hidden" name="ComponentAssignments[${assignmentIdx}].ComponentName" value="${comp}" />
                        <input type="hidden" name="ComponentAssignments[${assignmentIdx}].ProcessName" value="${p}" />
                    </td>
                `;
                assignmentIdx++;
            });

            tbody += `
                <td class="text-center">
                    <button type="button" class="row-action danger remove-lot-component-btn" data-name="${comp}" title="Remove ${comp}" aria-label="Remove ${comp}"><i class="bi bi-trash"></i></button>
                </td>
            </tr>`;
        });

        if (lotComponentsList.length === 0) {
            tbody = `<tr><td colspan="${procs.length + 2}" class="text-center text-muted py-4">No components defined for this lot. Click a quick add button or type a component name above.</td></tr>`;
        }

        $('#lotMatrixTableBody').html(tbody);
        syncLotLegacyWorkerFallbacks();
    }

    function syncLotLegacyWorkerFallbacks() {
        var hwVendor = '';
        var stVendor = '';
        var hasHwCholi = false;
        var hasHwChaniya = false;
        var hasHwDupatta = false;

        Object.keys(lotMatrixState).forEach(function(key) {
            var parts = key.split('___');
            var c = parts[0];
            var p = parts[1];
            var entry = lotMatrixState[key];
            if (entry && entry.vendorId) {
                if (p.toLowerCase().includes('handwork')) {
                    if (!hwVendor) hwVendor = entry.vendorId;
                    if (c.toLowerCase().includes('choli')) hasHwCholi = true;
                    if (c.toLowerCase().includes('chaniya') || c.toLowerCase().includes('lehenga')) hasHwChaniya = true;
                    if (c.toLowerCase().includes('dupatta')) hasHwDupatta = true;
                }
                if (p.toLowerCase().includes('stitching') && !stVendor) {
                    stVendor = entry.vendorId;
                }
            }
        });

        $('#fallbackHandworkWorkerId').val(hwVendor);
        $('#fallbackStitchingWorkerId').val(stVendor);
        $('#fallbackHwCholi').val(hasHwCholi ? 'true' : 'false');
        $('#fallbackHwChaniya').val(hasHwChaniya ? 'true' : 'false');
        $('#fallbackHwDupatta').val(hasHwDupatta ? 'true' : 'false');
    }

    $(document).on('change', '.lot-matrix-vendor-select', function() {
        var comp = $(this).data('comp');
        var proc = $(this).data('proc');
        var key = comp + '___' + proc;
        if (!lotMatrixState[key]) lotMatrixState[key] = {};
        lotMatrixState[key].vendorId = $(this).val();
        syncLotLegacyWorkerFallbacks();
    });

    $(document).on('input', '.lot-matrix-rate-input', function() {
        var comp = $(this).data('comp');
        var proc = $(this).data('proc');
        var key = comp + '___' + proc;
        if (!lotMatrixState[key]) lotMatrixState[key] = {};
        lotMatrixState[key].rate = $(this).val();
    });

    function addLotComponent(name) {
        name = (name || '').trim();
        if (!name) return;
        if (!lotComponentsList.some(function(c) { return c.toLowerCase() === name.toLowerCase(); })) {
            lotComponentsList.push(name);
            renderLotComponentBadges();
            renderLotMatrixTable();
        }
    }

    function removeLotComponent(name) {
        lotComponentsList = lotComponentsList.filter(function(c) { return c.toLowerCase() !== name.toLowerCase(); });
        Object.keys(lotMatrixState).forEach(function(k) {
            if (k.startsWith(name + '___')) delete lotMatrixState[k];
        });
        renderLotComponentBadges();
        renderLotMatrixTable();
    }

    $(document).on('click', '.quick-add-lot-comp', function() {
        var name = $(this).data('name');
        addLotComponent(name);
    });

    $('#btnAddCustomLotComponent').on('click', function() {
        var name = $('#customLotComponentInput').val();
        if (name) {
            addLotComponent(name);
            $('#customLotComponentInput').val('');
        }
    });

    $('#customLotComponentInput').on('keypress', function(e) {
        if (e.which === 13) {
            e.preventDefault();
            $('#btnAddCustomLotComponent').click();
        }
    });

    $(document).on('click', '.remove-lot-component-btn', function() {
        var name = $(this).data('name');
        removeLotComponent(name);
    });
});
