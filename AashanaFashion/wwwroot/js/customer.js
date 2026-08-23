$(document).ready(function () {
    $('#fetchGstBtn').click(function () {
        var gst = $('#gstInput').val().trim().toUpperCase();
        if (gst.length !== 15) {
            $('#gstError').text('GST number must be 15 characters').show();
            $('#gstSuccess').hide();
            return;
        }

        $('#gstError').hide();
        $('#fetchGstBtn').prop('disabled', true).text('Fetching...');

        $.ajax({
            url: '/Customer/VerifyGSTIN',
            type: 'GET',
            data: { gstin: gst },
            success: function (result) {
                if (result.success) {
                    $('#CustomerName').val(result.legalName || result.tradeName);
                    $('#GstNumber').val(gst);
                    $('#Address').val(result.address);
                    $('#City').val(result.city);
                    $('#State').val(result.state);
                    $('#PinCode').val(result.pinCode);
                    $('#PanNumber').val(result.panNumber);

                    $('#gstSuccess').text(result.message || 'GST details fetched successfully!').show();
                    $('#fetchGstBtn').prop('disabled', false).text('Fetch');
                } else {
                    $('#gstError').text(result.message || 'Failed to verify GSTIN').show();
                    $('#gstSuccess').hide();
                    $('#fetchGstBtn').prop('disabled', false).text('Fetch');
                }
            },
            error: function (xhr, status, error) {
                $('#gstError').text('Error connecting to verification service.').show();
                $('#gstSuccess').hide();
                $('#fetchGstBtn').prop('disabled', false).text('Fetch');
            }
        });
    });

    // Auto-uppercase GST input
    $('#gstInput').on('input', function () {
        this.value = this.value.toUpperCase();
    });

    // Contact row management
    function reindexContactRows() {
        $('#contactBody .contact-row').each(function (idx) {
            $(this).find('input').each(function () {
                var name = $(this).attr('name');
                if (name) {
                    name = name.replace(/^Contacts\[\d+\]/, 'Contacts[' + idx + ']');
                    $(this).attr('name', name);
                }
            });
            $(this).find('.remove-contact-row').toggle(idx > 0);
        });
    }

    $('#addContactRow').click(function () {
        var $last = $('#contactBody .contact-row').last();
        var $clone = $last.clone();
        $clone.find('input').val('');
        $clone.find('.remove-contact-row').show();
        $('#contactBody').append($clone);
        reindexContactRows();
    });

    $(document).on('click', '.remove-contact-row', function () {
        if ($('#contactBody .contact-row').length > 1) {
            $(this).closest('tr').remove();
            reindexContactRows();
        }
    });

    // Initial reindex to ensure contiguous indices
    reindexContactRows();
});
