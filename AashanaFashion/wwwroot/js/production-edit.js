$(document).ready(function () {
    let currentColours = [];
    let currentSizes = [];
    let detailIndex = 0;

    function init() {
        var selected = $('#designSelect').find(':selected');
        var colours = selected.data('colours') || '';
        var sizes = selected.data('sizes') || '';

        currentColours = colours.split(',').map(c => c.trim()).filter(c => c);
        currentSizes = sizes.split(',').map(s => s.trim()).filter(s => s);

        generateMatrix();
    }

    $('#designSelect').change(function () {
        init();
    });

    function generateMatrix() {
        var tbody = $('#matrixBody');
        var theadRow = $('#matrixTable thead tr');
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
            theadRow.append('<th class="text-center">' + size + '</th>');
        });
        theadRow.append('<th class="text-center">Row Total</th>');

        // Generate matrix rows
        currentColours.forEach(function (colour) {
            var row = $('<tr>');
            row.append('<td><strong>' + colour + '</strong></td>');

            currentSizes.forEach(function (size) {
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
                    '<input type="number" class="matrix-input" data-row="' + colour + '" data-col="' + size + '" ' +
                    'name="Details[' + detailIndex + '].Quantity" ' +
                    'min="0" value="' + existingQty + '" style="width:60px;text-align:center;" />' +
                    '</td>'
                );
                detailIndex++;
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

        // Initial calculation
        updateTotals();
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

    // Initialize on page load
    init();
});
