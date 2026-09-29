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

    // Commission row management
    function initCommissionRowEvents($row) {
        var $basis = $row.find('.comm-basis-select');
        var $cat = $row.find('.comm-category-select');
        var $des = $row.find('.comm-design-select');
        var $text = $row.find('.comm-text-target');
        var $targetVal = $row.find('.comm-target-val');
        var $desId = $row.find('.comm-design-id');
        var $calcType = $row.find('.comm-calctype-select');
        var $unit = $row.find('.comm-rate-unit');

        function updateBasisUI() {
            var b = $basis.val();
            if (b === '0') { // Category
                $cat.removeClass('d-none');
                $des.addClass('d-none');
                $text.addClass('d-none');
                $targetVal.val($cat.val() || '');
                $desId.val('');
            } else if (b === '1') { // AllProducts
                $cat.addClass('d-none');
                $des.addClass('d-none');
                $text.removeClass('d-none').val('All Products').prop('readonly', true);
                $targetVal.val('All Products');
                $desId.val('');
            } else if (b === '2') { // Design
                $cat.addClass('d-none');
                $des.removeClass('d-none');
                $text.addClass('d-none');
                var opt = $des.find('option:selected');
                $targetVal.val(opt.val() || '');
                $desId.val(opt.data('id') || '');
            } else if (b === '3') { // FixedPerPiece
                $cat.removeClass('d-none');
                $des.addClass('d-none');
                $text.addClass('d-none');
                $targetVal.val($cat.val() || '');
                $desId.val('');
                $calcType.val('1'); // Fixed Amount
                $unit.text('₹');
            }
        }

        $basis.off('change.comm').on('change.comm', updateBasisUI);

        $cat.off('change.comm').on('change.comm', function () {
            if ($basis.val() === '0' || $basis.val() === '3') {
                $targetVal.val($(this).val() || '');
            }
        });

        $des.off('change.comm').on('change.comm', function () {
            if ($basis.val() === '2') {
                var opt = $(this).find('option:selected');
                $targetVal.val(opt.val() || '');
                $desId.val(opt.data('id') || '');
            }
        });

        $calcType.off('change.comm').on('change.comm', function () {
            $unit.text($(this).val() === '1' ? '₹' : '%');
        });
    }

    function reindexCommissionRows() {
        $('#commissionBody .commission-row').each(function (idx) {
            $(this).find('[name^="Commissions["]').each(function () {
                var name = $(this).attr('name');
                if (name) {
                    name = name.replace(/^Commissions\[\d+\]/, 'Commissions[' + idx + ']');
                    $(this).attr('name', name);
                }
            });
            initCommissionRowEvents($(this));
        });
    }

    $('#commissionBody .commission-row').each(function () {
        initCommissionRowEvents($(this));
    });

    $('#btnAddCommissionRow').click(function () {
        var $first = $('#commissionBody .commission-row').first();
        var $clone = $first.clone();
        $clone.find('input[type="text"], input[type="number"], input[type="date"], select').each(function () {
            if ($(this).hasClass('comm-basis-select')) {
                $(this).val('0'); // default to Category
            } else if ($(this).hasClass('comm-calctype-select')) {
                $(this).val('0'); // default to %
            } else if ($(this).hasClass('comm-rate-input')) {
                $(this).val('0.00');
            } else if ($(this).hasClass('form-check-input')) {
                $(this).prop('checked', true);
            } else {
                $(this).val('');
            }
        });
        $clone.find('.comm-target-val').val('');
        $clone.find('.comm-design-id').val('');
        $clone.find('.comm-rate-unit').text('%');
        $clone.find('.comm-category-select').removeClass('d-none').val('');
        $clone.find('.comm-design-select').addClass('d-none').val('');
        $clone.find('.comm-text-target').addClass('d-none').val('');

        $('#commissionBody').append($clone);
        reindexCommissionRows();
    });

    $(document).on('click', '.remove-commission-row', function () {
        if ($('#commissionBody .commission-row').length > 1) {
            $(this).closest('tr').remove();
            reindexCommissionRows();
        } else {
            // If only 1 row, clear it
            var $row = $(this).closest('tr');
            $row.find('input[type="text"], input[type="number"], input[type="date"]').val('');
            $row.find('.comm-basis-select').val('0').trigger('change');
            $row.find('.comm-calctype-select').val('0').trigger('change');
            $row.find('.comm-rate-input').val('0.00');
        }
    });

    // Initial reindex to ensure contiguous indices
    reindexContactRows();
    reindexCommissionRows();
});
