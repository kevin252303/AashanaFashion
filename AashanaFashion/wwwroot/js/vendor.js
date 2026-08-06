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
        // For demo, we'll auto-fill with sample data
        setTimeout(function () {
            // Simulated vendor data
            var vendorData = {
                vendorName: 'Sample Vendor ' + gst.substring(2, 5),
                gstNumber: gst,
                address: '123 Business Park, MG Road',
                city: 'Mumbai',
                state: 'Maharashtra',
                pinCode: '400001',
                email: 'contact@vendor.com',
                phone: '9876543210'
            };

            // Auto-fill the form
            $('#VendorName').val(vendorData.vendorName);
            $('#GstNumber').val(vendorData.gstNumber);
            $('#Address').val(vendorData.address);
            $('#City').val(vendorData.city);
            $('#State').val(vendorData.state);
            $('#PinCode').val(vendorData.pinCode);
            $('#Email').val(vendorData.email);
            $('#Phone').val(vendorData.phone);

            $('#gstSuccess').text('GST details fetched successfully!').show();
            $('#fetchGstBtn').prop('disabled', false).text('Fetch');
        }, 1000);
    });

    // Auto-uppercase GST input
    $('#gstInput').on('input', function () {
        this.value = this.value.toUpperCase();
    });
});
