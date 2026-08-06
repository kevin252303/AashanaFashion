$(document).ready(function () {
    let currentColours = [];
    let currentSizes = [];

    $('#designSelect').change(function () {
        var selected = $(this).find(':selected');
        var colours = selected.data('colours') || '';
        var sizes = selected.data('sizes') || '';
        var steps = selected.data('steps') || '';

        currentColours = colours.split(',').map(c => c.trim()).filter(c => c);
        currentSizes = sizes.split(',').map(s => s.trim()).filter(s => s);

        generateMatrix();
        //updateVerificationSteps(steps);
    });

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
            row.append('<td><strong>' + colour + '</strong></td>');

            var rowIndex = currentColours.indexOf(colour);

            currentSizes.forEach(function (size, sizeIdx) {
                var detailIndex = rowIndex * currentSizes.length + sizeIdx;
                row.append(
                    '<td class="text-center">' +
                    '<input type="hidden" name="Details[' + detailIndex + '].Colour" value="' + colour + '" />' +
                    '<input type="hidden" name="Details[' + detailIndex + '].Size" value="' + size + '" />' +
                    '<input type="number" class="matrix-input" data-row="' + rowIndex + '" data-col="' + sizeIdx + '" ' +
                    'name="Details[' + detailIndex + '].Quantity" ' +
                    'min="0" value="0" style="width:70px;text-align:center;padding:.4rem;" />' +
                    '</td>'
                );
            });

            // Row total
            row.append('<td class="text-center" style="background:#f0fdf4;"><strong class="row-total" data-row="' + rowIndex + '">0</strong></td>');
            tbody.append(row);
        });

        // Add column totals row
        var totalRow = $('<tr style="background:#f8fafc;font-weight:bold;">');
        totalRow.append('<td><strong>Total</strong></td>');
        currentSizes.forEach(function (size, sizeIdx) {
            totalRow.append('<td class="text-center" style="background:#f0fdf4;"><strong class="col-total" data-col="' + sizeIdx + '">0</strong></td>');
        });
        totalRow.append('<td class="text-center" style="background:#dcfce7;"><strong id="grandTotal">0</strong></td>');
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
                    '<label style="display:flex;align-items:center;gap:.6rem;cursor:pointer;">' +
                    '<input type="checkbox" name="' + fieldName + '" style="accent-color:#6366f1;width:16px;height:16px;" />' +
                    step + ' Verified' +
                    '</label>'
                );
            }
        });
    }
});
