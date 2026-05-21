document.addEventListener('DOMContentLoaded', function () {
    const deleteModal = document.getElementById('deleteReceiptModal');
    const deleteReceiptId = document.getElementById('deleteReceiptId');
    const deleteReceiptName = document.getElementById('deleteReceiptName');

    if (!deleteModal || !deleteReceiptId || !deleteReceiptName) {
        return;
    }

    deleteModal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;

        if (!button) {
            return;
        }

        deleteReceiptId.value = button.getAttribute('data-receipt-id') || '';
        deleteReceiptName.textContent = button.getAttribute('data-receipt-name') || 'this receipt';
    });
});