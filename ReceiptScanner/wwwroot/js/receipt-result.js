document.addEventListener('DOMContentLoaded', function () {
    const addItemBtn = document.getElementById('addItemBtn');
    const itemsBody = document.getElementById('itemsBody');
    const itemRowTemplate = document.getElementById('itemRowTemplate');
    const emptyItemsMessage = document.getElementById('emptyItemsMessage');

    if (!addItemBtn || !itemsBody || !itemRowTemplate) {
        return;
    }

    addItemBtn.addEventListener('click', function () {
        const clone = itemRowTemplate.content.cloneNode(true);
        itemsBody.appendChild(clone);
        refreshItemIndexes();

        const lastRow = itemsBody.querySelector('tr:last-child');
        const nameInput = lastRow?.querySelector('.item-name');

        if (nameInput) {
            nameInput.focus();
        }
    });

    itemsBody.addEventListener('click', function (event) {
        const removeButton = event.target.closest('.remove-item-btn');

        if (!removeButton) {
            return;
        }

        const row = removeButton.closest('tr');
        row?.remove();
        refreshItemIndexes();
    });

    refreshItemIndexes();

    function refreshItemIndexes() {
        const rows = itemsBody.querySelectorAll('tr.receipt-item-row');

            rows.forEach(function (row, index) {
                setInputName(row, index, 'ItemId');
                setInputName(row, index, 'ReceiptId');
                setInputName(row, index, 'Name');
                setInputName(row, index, 'Quantity');
                setInputName(row, index, 'UnitPrice');
                setInputName(row, index, 'TotalPrice');
                setInputName(row, index, 'CategoryId');
        });

        if (emptyItemsMessage) {
            emptyItemsMessage.classList.toggle('d-none', rows.length > 0);
        }
    }

    function setInputName(row, index, fieldName) {
        const input =
            row.querySelector(`[data-field="${fieldName}"]`) ||
            row.querySelector(`[name^="Items["][name$=".${fieldName}"]`);

        if (!input) {
            return;
        }

        input.name = `Items[${index}].${fieldName}`;
        input.id = `Items_${index}__${fieldName}`;

        if (fieldName === 'ItemId' && !input.value) {
            input.value = createId();
        }
    }

    function createId() {
        if (crypto.randomUUID) {
            return crypto.randomUUID();
        }

        return 'item-' + Date.now() + '-' + Math.random().toString(16).slice(2);
    }

    document.addEventListener("click", function (e) {
        if (!e.target.classList.contains("apply-total-btn"))
            return;

        const button = e.target;
        const td = button.closest("td");

        const input = td.querySelector(".item-total-price");

        if (!input)
            return;

        input.value = button.dataset.total;

        input.classList.remove("suggested-field");

        const warning = button.closest(".form-text");
        if (warning) {
            warning.remove();
        }
    });

    let allowSubmitWithMismatch = false;

    const totalEurInput = document.querySelector('[name="TotalEUR"]');
    const form = totalEurInput ? totalEurInput.closest('form') : null;
    const mismatchModalElement = document.getElementById('totalMismatchModal');
    const confirmMismatchSaveBtn = document.getElementById('confirmMismatchSaveBtn');

    if (form && totalEurInput && mismatchModalElement && confirmMismatchSaveBtn) {
        const mismatchModal = new bootstrap.Modal(mismatchModalElement);

        form.addEventListener('submit', function (event) {
            if (allowSubmitWithMismatch) {
                return;
            }

            const totalEur = parseDecimal(totalEurInput.value);
            const itemsTotal = getItemsTotalEur();

            if (totalEur === null || itemsTotal === null) {
                return;
            }

            const difference = roundToTwo(totalEur - itemsTotal);

            if (Math.abs(difference) <= 0.01) {
                return;
            }

            event.preventDefault();

            document.getElementById('receiptTotalEurText').textContent = formatMoney(totalEur);
            document.getElementById('itemsTotalEurText').textContent = formatMoney(itemsTotal);
            document.getElementById('totalDifferenceEurText').textContent = formatMoney(difference);

            mismatchModal.show();
        });

        confirmMismatchSaveBtn.addEventListener('click', function () {
            allowSubmitWithMismatch = true;
            mismatchModal.hide();
            form.requestSubmit();
        });
    }

    function getItemsTotalEur() {
        const itemTotalInputs = document.querySelectorAll('.item-total-price');

        if (itemTotalInputs.length === 0) {
            return null;
        }

        let total = 0;

        itemTotalInputs.forEach(function (input) {
            const value = parseDecimal(input.value);

            if (value !== null) {
                total += value;
            }
        });

        return roundToTwo(total);
    }

    function parseDecimal(value) {
        if (!value) {
            return null;
        }

        const normalized = value.replace(',', '.');
        const parsed = Number.parseFloat(normalized);

        if (Number.isNaN(parsed)) {
            return null;
        }

        return parsed;
    }

    function roundToTwo(value) {
        return Math.round((value + Number.EPSILON) * 100) / 100;
    }

    function formatMoney(value) {
        return value.toFixed(2);
    }
});
