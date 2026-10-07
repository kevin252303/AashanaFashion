/**
 * Aashana Fashion — IFSC Bank & Branch Auto-Lookup Helper (IGNEK Pulse)
 * Standardizes bank name and branch lookup across Company, Vendor, Customer, Employee, Invoice, and Bank Payment views.
 */
(function () {
    'use strict';

    /**
     * Look up IFSC code via backend API endpoint.
     * @param {string} ifsc 
     * @returns {Promise<object>}
     */
    window.lookupIfscCode = async function (ifsc) {
        if (!ifsc) return { success: false, message: 'IFSC code is required.' };
        const clean = ifsc.trim().toUpperCase();
        try {
            const response = await fetch('/api/bank/lookup-ifsc?ifsc=' + encodeURIComponent(clean), {
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) {
                return { success: false, message: 'Server returned error ' + response.status };
            }
            return await response.json();
        } catch (err) {
            return { success: false, message: 'Network error connecting to bank lookup service.' };
        }
    };

    /**
     * Bind auto-lookup to IFSC input, Bank Name input, Branch input, and optional trigger button & status container.
     * @param {object} opts
     * @param {string|HTMLElement} opts.ifscInput
     * @param {string|HTMLElement} [opts.bankNameInput]
     * @param {string|HTMLElement} [opts.branchInput]
     * @param {string|HTMLElement} [opts.fetchBtn]
     * @param {string|HTMLElement} [opts.statusContainer]
     * @param {function} [opts.onSuccess]
     */
    window.initIfscLookup = function (opts) {
        const ifscEl = typeof opts.ifscInput === 'string' ? document.querySelector(opts.ifscInput) : opts.ifscInput;
        if (!ifscEl) return;

        const bankNameEl = typeof opts.bankNameInput === 'string' ? document.querySelector(opts.bankNameInput) : opts.bankNameInput;
        const branchEl = typeof opts.branchInput === 'string' ? document.querySelector(opts.branchInput) : opts.branchInput;
        const btnEl = typeof opts.fetchBtn === 'string' ? document.querySelector(opts.fetchBtn) : opts.fetchBtn;
        let statusEl = typeof opts.statusContainer === 'string' ? document.querySelector(opts.statusContainer) : opts.statusContainer;

        // Create status element if not provided
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.className = 'ifsc-status-hint text-xs mt-1';
            statusEl.style.display = 'none';
            ifscEl.parentElement.appendChild(statusEl);
        }

        let lastCheckedIfsc = '';
        let isChecking = false;

        function setStatus(type, msg, icon) {
            if (!statusEl) return;
            statusEl.style.display = 'block';
            statusEl.className = 'ifsc-status-hint text-xs mt-1 ' + (type === 'success' ? 'text-success' : type === 'error' ? 'text-danger' : 'text-muted');
            statusEl.innerHTML = '<i class="bi ' + (icon || (type === 'success' ? 'bi-check-circle' : type === 'error' ? 'bi-exclamation-circle' : 'bi-info-circle')) + ' me-1"></i><span>' + msg + '</span>';
        }

        function clearStatus() {
            if (statusEl) {
                statusEl.innerHTML = '';
                statusEl.style.display = 'none';
            }
        }

        async function doLookup(isExplicitClick) {
            const val = ifscEl.value.trim().toUpperCase();
            ifscEl.value = val;

            if (!val) {
                clearStatus();
                return;
            }

            if (val.length < 11 && !isExplicitClick) {
                clearStatus();
                return;
            }

            if (val === lastCheckedIfsc && !isExplicitClick) {
                return;
            }

            if (val.length !== 11) {
                setStatus('error', 'IFSC must be exactly 11 characters (e.g. SBIN0001234, HDFC0000240)');
                return;
            }

            if (isChecking) return;
            isChecking = true;
            lastCheckedIfsc = val;

            if (btnEl) {
                btnEl.disabled = true;
                btnEl.setAttribute('data-original-html', btnEl.innerHTML);
                btnEl.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Fetching...';
            }
            setStatus('loading', 'Looking up bank & branch details...', 'bi-arrow-repeat');

            try {
                const res = await window.lookupIfscCode(val);

                if (res.success) {
                    if (bankNameEl && res.bankName) {
                        bankNameEl.value = res.bankName;
                        // Flash light highlight
                        bankNameEl.classList.add('is-valid');
                        setTimeout(() => bankNameEl.classList.remove('is-valid'), 2500);
                    }
                    if (branchEl && res.branch) {
                        branchEl.value = res.branch;
                        branchEl.classList.add('is-valid');
                        setTimeout(() => branchEl.classList.remove('is-valid'), 2500);
                    }

                    const branchText = res.branch ? ' (' + res.branch + ')' : '';
                    setStatus('success', (res.bankName || 'Bank') + branchText + ' verified.');

                    if (typeof opts.onSuccess === 'function') {
                        opts.onSuccess(res);
                    }
                } else {
                    setStatus('error', res.message || 'IFSC code not found in bank directory.');
                }
            } catch (err) {
                setStatus('error', 'Failed to fetch bank details.');
            } finally {
                isChecking = false;
                if (btnEl) {
                    btnEl.disabled = false;
                    const orig = btnEl.getAttribute('data-original-html');
                    if (orig) btnEl.innerHTML = orig;
                }
            }
        }

        // Auto uppercase as user types
        ifscEl.addEventListener('input', function () {
            this.value = this.value.toUpperCase();
            if (this.value.length === 11) {
                doLookup(false);
            } else {
                clearStatus();
            }
        });

        // Trigger on blur
        ifscEl.addEventListener('blur', function () {
            if (this.value.length === 11) {
                doLookup(false);
            }
        });

        // Trigger on fetch button click
        if (btnEl) {
            btnEl.addEventListener('click', function (e) {
                e.preventDefault();
                doLookup(true);
            });
        }
    };

    // Auto-initialize any input marked with data-ifsc-lookup="true"
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-ifsc-lookup="true"]').forEach(function (el) {
            const bankTarget = el.getAttribute('data-target-bank');
            const branchTarget = el.getAttribute('data-target-branch');
            const btnTarget = el.getAttribute('data-target-btn');
            const statusTarget = el.getAttribute('data-target-status');

            window.initIfscLookup({
                ifscInput: el,
                bankNameInput: bankTarget ? document.querySelector(bankTarget) : null,
                branchInput: branchTarget ? document.querySelector(branchTarget) : null,
                fetchBtn: btnTarget ? document.querySelector(btnTarget) : null,
                statusContainer: statusTarget ? document.querySelector(statusTarget) : null
            });
        });
    });
})();
