$(document).ready(function () {
    let currentColours = [];
    let currentSizes = [];

    $('#designSelect').change(function () {
        var selected = $(this).find(':selected');
        var colours = selected.data('colours') || '';
        var sizes = selected.data('sizes') || '';

        currentColours = colours.split(',').map(c => c.trim()).filter(c => c);
        currentSizes = sizes.split(',').map(s => s.trim()).filter(s => s);

        generateMatrix();
    });

    function generateMatrix() {
        var tbody = $('#matrixBody');
        var theadRow = $('#matrixTable thead tr');
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
            theadRow.append('<th class="text-center">' + size + '</th>');
        });
        theadRow.append('<th class="text-center">Row Total</th>');

        // Generate matrix rows
        currentColours.forEach(function (colour) {
            var row = $('<tr>');
            row.append('<td><strong>' + colour + '</strong></td>');

            var rowTotal = 0;
            currentSizes.forEach(function (size) {
                var index = currentColours.indexOf(colour) + '_' + currentSizes.indexOf(size);
                row.append(
                    '<td class="text-center">' +
                    '<input type="number" name="Details[' + currentColours.indexOf(colour) + '].Colour" value="' + colour + '" hidden />' +
                    '<input type="number" name="Details[' + currentSizes.indexOf(size) + '].Size" value="' + size + '" hidden />' +
                    '<input type="number" class="matrix-input" data-row="' + colour + '" data-col="' + size + '" ' +
                    'name="Details[' + (currentColours.indexOf(colour) * currentSizes.length + currentSizes.indexOf(size)) + '].Quantity" ' +
                    'min="0" value="0" style="width:60px;text-align:center;" />' +
                    '</td>'
                );
            });

            row.append('<td class="text-center"><strong class="row-total" data-row="' + colour + '">0</strong></td>');
            tbody.append(row);
        });

        // Add column totals row
        var totalRow = $('<tr style="background:#f8fafc;font-weight:bold;">');
        totalRow.append('<td><strong>Column Total</strong></td>');
        currentSizes.forEach(function (size) {
            totalRow.append('<td class="text-center"><strong class="col-total" data-col="' + size + '">0</strong></td>');
        });
        totalRow.append('<td class="text-center"><strong id="grandTotal">0</strong></td>');
        tbody.append(totalRow);

        // Add event listeners
        $('.matrix-input').on('input', function () {
            updateTotals();
        });
    }

    function updateTotals() {
        var grandTotal = 0;

        // Row totals
        currentColours.forEach(function (colour) {
            var rowTotal = 0;
            $('.matrix-input[data-row="' + colour + '"]').each(function () {
                rowTotal += parseInt($(this).val()) || 0;
            });
            $('.row-total[data-row="' + colour + '"]').text(rowTotal);
        });

        // Column totals
        currentSizes.forEach(function (size) {
            var colTotal = 0;
            $('.matrix-input[data-col="' + size + '"]').each(function () {
                colTotal += parseInt($(this).val()) || 0;
            });
            $('.col-total[data-col="' + size + '"]').text(colTotal);
        });

        // Grand total
        $('.matrix-input').each(function () {
            grandTotal += parseInt($(this).val()) || 0;
        });

        $('#grandTotal').text(grandTotal);
        $('#totalPieces').text('Total: ' + grandTotal + ' pcs');
        $('#totalQuantity').val(grandTotal);
    }
});
