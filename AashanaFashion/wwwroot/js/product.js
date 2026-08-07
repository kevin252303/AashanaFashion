(function () {
    // ——— Attribute rows ———
    var attrBody = document.getElementById('attrBody');
    var addAttrBtn = document.getElementById('addAttrRow');

    if (addAttrBtn && attrBody) {
        function createAttrRow(index) {
            var tr = document.createElement('tr');
            tr.className = 'attr-row';
            tr.innerHTML =
                '<td><input name="AttributeLines[' + index + '].Attribute" class="af-input" placeholder="e.g. Size, Color" /></td>' +
                '<td><input name="AttributeLines[' + index + '].Values" class="af-input" placeholder="e.g. S,M,L / Red,Blue" /></td>' +
                '<td class="text-center"><input name="AttributeLines[' + index + '].ColourCheck" type="checkbox" value="true" /><input type="hidden" name="AttributeLines[' + index + '].ColourCheck" value="false" /></td>' +
                '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-attr-row">\u00D7</button></td>';
            return tr;
        }

        function reindexAttr() {
            var rows = attrBody.querySelectorAll('.attr-row');
            rows.forEach(function (row, idx) {
                row.querySelectorAll('input').forEach(function (el) {
                    var name = el.getAttribute('name');
                    if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
                });
                var btn = row.querySelector('.remove-attr-row');
                if (btn) btn.style.display = rows.length > 1 ? '' : 'none';
            });
        }

        function bindAttrRemove(row) {
            var btn = row.querySelector('.remove-attr-row');
            if (btn) {
                btn.addEventListener('click', function () {
                    if (attrBody.querySelectorAll('.attr-row').length > 1) {
                        row.remove();
                        reindexAttr();
                    }
                });
            }
        }

        addAttrBtn.addEventListener('click', function () {
            var count = attrBody.querySelectorAll('.attr-row').length;
            var row = createAttrRow(count);
            attrBody.appendChild(row);
            bindAttrRemove(row);
            reindexAttr();
        });

        attrBody.querySelectorAll('.attr-row').forEach(bindAttrRemove);
        reindexAttr();
    }

    // ——— Pricelist rows ———
    var priceBody = document.getElementById('priceBody');
    var addPriceBtn = document.getElementById('addPriceRow');

    if (addPriceBtn && priceBody) {
        function createPriceRow(index) {
            var tr = document.createElement('tr');
            tr.className = 'price-row';
            tr.innerHTML =
                '<td><input name="Pricelists[' + index + '].Pricelist" class="af-input" placeholder="Pricelist name" /></td>' +
                '<td><input name="Pricelists[' + index + '].AppliedOn" class="af-input" placeholder="e.g. Product, Category" /></td>' +
                '<td><input name="Pricelists[' + index + '].Price" class="af-input" type="number" step="0.01" min="0" value="0" /></td>' +
                '<td><input name="Pricelists[' + index + '].MinQuantity" class="af-input" type="number" step="1" min="0" value="1" /></td>' +
                '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-price-row">\u00D7</button></td>';
            return tr;
        }

        function reindexPrice() {
            var rows = priceBody.querySelectorAll('.price-row');
            rows.forEach(function (row, idx) {
                row.querySelectorAll('input').forEach(function (el) {
                    var name = el.getAttribute('name');
                    if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
                });
                var btn = row.querySelector('.remove-price-row');
                if (btn) btn.style.display = rows.length > 1 ? '' : 'none';
            });
        }

        function bindPriceRemove(row) {
            var btn = row.querySelector('.remove-price-row');
            if (btn) {
                btn.addEventListener('click', function () {
                    if (priceBody.querySelectorAll('.price-row').length > 1) {
                        row.remove();
                        reindexPrice();
                    }
                });
            }
        }

        addPriceBtn.addEventListener('click', function () {
            var count = priceBody.querySelectorAll('.price-row').length;
            var row = createPriceRow(count);
            priceBody.appendChild(row);
            bindPriceRemove(row);
            reindexPrice();
        });

        priceBody.querySelectorAll('.price-row').forEach(bindPriceRemove);
        reindexPrice();
    }

    // ——— Vendor rows ———
    var vendorBody = document.getElementById('vendorBody');
    var addVendorBtn = document.getElementById('addVendorRow');

    if (addVendorBtn && vendorBody) {
        function createVendorRow(index) {
            var tr = document.createElement('tr');
            tr.className = 'vendor-row';
            tr.innerHTML =
                '<td><select name="ProductVendors[' + index + '].VendorId" class="af-input af-select">' +
                    '<option value="">\u2014 Select \u2014</option>' +
                    document.querySelector('#vendorBody .vendor-row select')?.innerHTML.replace(/selected/g, '') +
                '</select></td>' +
                '<td><input name="ProductVendors[' + index + '].Quantity" class="af-input" type="number" step="1" min="0" value="1" /></td>' +
                '<td><select name="ProductVendors[' + index + '].Unit" class="af-input af-select">' +
                    '<option value="Piece">Piece</option>' +
                    '<option value="Meter">Meter</option>' +
                '</select></td>' +
                '<td><input name="ProductVendors[' + index + '].UnitPrice" class="af-input" type="number" step="0.01" min="0" value="0" /></td>' +
                '<td><input name="ProductVendors[' + index + '].LeadTime" class="af-input" type="number" min="0" value="0" /></td>' +
                '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-vendor-row">\u00D7</button></td>';
            return tr;
        }

        function reindexVendor() {
            var rows = vendorBody.querySelectorAll('.vendor-row');
            rows.forEach(function (row, idx) {
                row.querySelectorAll('input, select').forEach(function (el) {
                    var name = el.getAttribute('name');
                    if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
                });
                var btn = row.querySelector('.remove-vendor-row');
                if (btn) btn.style.display = rows.length > 1 ? '' : 'none';
            });
        }

        function bindVendorRemove(row) {
            var btn = row.querySelector('.remove-vendor-row');
            if (btn) {
                btn.addEventListener('click', function () {
                    if (vendorBody.querySelectorAll('.vendor-row').length > 1) {
                        row.remove();
                        reindexVendor();
                    }
                });
            }
        }

        addVendorBtn.addEventListener('click', function () {
            var count = vendorBody.querySelectorAll('.vendor-row').length;
            var row = createVendorRow(count);
            vendorBody.appendChild(row);
            bindVendorRemove(row);
            reindexVendor();
        });

        vendorBody.querySelectorAll('.vendor-row').forEach(bindVendorRemove);
        reindexVendor();
    }

    // ——— Packaging rows ———
    var pkgBody = document.getElementById('pkgBody');
    var addPkgBtn = document.getElementById('addPkgRow');

    if (addPkgBtn && pkgBody) {
        function createPkgRow(index) {
            var tr = document.createElement('tr');
            tr.className = 'pkg-row';
            tr.innerHTML =
                '<td><input name="Packagings[' + index + '].PackagingName" class="af-input" placeholder="Packaging" /></td>' +
                '<td><input name="Packagings[' + index + '].Quantity" class="af-input" type="number" step="1" min="0" value="1" /></td>' +
                '<td><button type="button" class="btn-af btn-af-danger btn-af-sm remove-pkg-row">\u00D7</button></td>';
            return tr;
        }

        function reindexPkg() {
            var rows = pkgBody.querySelectorAll('.pkg-row');
            rows.forEach(function (row, idx) {
                row.querySelectorAll('input').forEach(function (el) {
                    var name = el.getAttribute('name');
                    if (name) el.setAttribute('name', name.replace(/\[\d+\]/, '[' + idx + ']'));
                });
                var btn = row.querySelector('.remove-pkg-row');
                if (btn) btn.style.display = rows.length > 1 ? '' : 'none';
            });
        }

        function bindPkgRemove(row) {
            var btn = row.querySelector('.remove-pkg-row');
            if (btn) {
                btn.addEventListener('click', function () {
                    if (pkgBody.querySelectorAll('.pkg-row').length > 1) {
                        row.remove();
                        reindexPkg();
                    }
                });
            }
        }

        addPkgBtn.addEventListener('click', function () {
            var count = pkgBody.querySelectorAll('.pkg-row').length;
            var row = createPkgRow(count);
            pkgBody.appendChild(row);
            bindPkgRemove(row);
            reindexPkg();
        });

        pkgBody.querySelectorAll('.pkg-row').forEach(bindPkgRemove);
        reindexPkg();
    }
})();
