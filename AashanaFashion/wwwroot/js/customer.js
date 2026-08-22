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

        // Simulated GST API response (replace with actual API call)
        setTimeout(function () {
            var customerData = {
                customerName: 'Sample Customer ' + gst.substring(2, 5),
                gstNumber: gst,
                address: '123 Business Park, MG Road',
                city: 'Mumbai',
                state: 'Maharashtra',
                pinCode: '400001',
                email: 'contact@customer.com',
                phone: '9876543210'
            };

            $('#CustomerName').val(customerData.customerName);
            $('#GstNumber').val(customerData.gstNumber);
            $('#Address').val(customerData.address);
            $('#City').val(customerData.city);
            $('#State').val(customerData.state);
            $('#PinCode').val(customerData.pinCode);
            $('#Email').val(customerData.email);
            $('#Phone').val(customerData.phone);

            $('#gstSuccess').text('GST details fetched successfully!').show();
            $('#fetchGstBtn').prop('disabled', false).text('Fetch');
        }, 1000);
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
