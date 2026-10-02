$(document).ready(function () {
    let currentColours = [];
    let currentSizes = [];
    let detailIndex = 0;

    var verificationMap = {
        'Raw Material': 'IsRawMaterialVerified',
        'Dying': 'IsDyingVerified',
        'Handwork': 'IsHandworkVerified',
        'Stitching': 'IsStitchingVerified'
    };

    var initialVerification = {
        IsRawMaterialVerified: false,
        IsDyingVerified: false,
        IsHandworkVerified: false,
        IsStitchingVerified: false
    };

    $('input[type="checkbox"][name^="Is"]').each(function() {
        if (this.name in initialVerification) {
            initialVerification[this.name] = this.checked;
        }
    });

    function init() {
        var selected = $('#designSelect').find(':selected');
        var colours = selected.data('colours') || '';
        var sizes = selected.data('sizes') || '';
        var steps = selected.data('steps') || '';

        currentColours = colours.split(',').map(c => c.trim()).filter(c => c);
        currentSizes = sizes.split(',').map(s => s.trim()).filter(s => s);

        generateMatrix();
        updateVerificationSteps(steps, true);
    }

    $('#designSelect').change(function () {
        var steps = $(this).find(':selected').data('steps') || '';
        init();
        updateVerificationSteps(steps, false);
    });

    function generateMatrix() {
        var tbody = $('#matrixBody');
        var theadRow = $('#matrixHeader');
        tbody.empty();
        detailIndex = 0;

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
        currentColours.forEach(function (colour, rowIdx) {
            var row = $('<tr>');
            row.append('<td class="cell-title">' + colour + '</td>');

            currentSizes.forEach(function (size, sizeIdx) {
                var existingQty = 0;
                if (existingDetails) {
                    var match = existingDetails.find(function (d) {
                        return d.colour === colour && d.size === size;
                    });
                    if (match) existingQty = match.quantity;
                }

                row.append(
                    '<td class="text-center">' +
                    '<input type="hidden" name="Details[' + detailIndex + '].Colour" value="' + colour + '" />' +
                    '<input type="hidden" name="Details[' + detailIndex + '].Size" value="' + size + '" />' +
                    '<input type="number" class="af-input af-input-sm matrix-input" data-row="' + rowIdx + '" data-col="' + sizeIdx + '" ' +
                    'name="Details[' + detailIndex + '].Quantity" ' +
                    'min="0" value="' + existingQty + '" />' +
                    '</td>'
                );
                detailIndex++;
            });

            // Row total
            row.append('<td class="cell-total"><span class="row-total" data-row="' + rowIdx + '">0</span></td>');
            tbody.append(row);
        });

        // Add column totals row
        var totalRow = $('<tr class="matrix-total-row">');
        totalRow.append('<td>Total</td>');
        currentSizes.forEach(function (size, colIdx) {
            totalRow.append('<td><span class="col-total" data-col="' + colIdx + '">0</span></td>');
        });
        totalRow.append('<td class="grand-total"><span id="grandTotal">0</span></td>');
        tbody.append(totalRow);

        // Add event listeners
        $('.matrix-input').on('input', function () {
            updateTotals();
        });

        // Initial calculation
        updateTotals();
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

    function updateVerificationSteps(steps, useInitialValues = false) {
        var container = $('#verificationContainer');
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
                var isChecked = useInitialValues && initialVerification[fieldName];
                container.append(
                    '<label class="check-item">' +
                    '<input type="checkbox" class="form-check-input m-0" name="' + fieldName + '" ' + (isChecked ? 'checked' : '') + ' />' +
                    step + ' Verified' +
                    '</label>'
                );
            }
        });
    }

    // Initialize on page load
    init();
});
