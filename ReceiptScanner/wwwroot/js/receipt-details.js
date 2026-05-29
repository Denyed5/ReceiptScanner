document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById('receiptImageModal');
    const modalBody = modal ? modal.querySelector('.receipt-image-modal-body') : null;
    const image = document.getElementById('receiptImageFull');
    const zoomButtons = document.querySelectorAll('[data-receipt-zoom]');

    if (!modal || !modalBody || !image || zoomButtons.length === 0) {
        return;
    }

    let zoom = 1;
    let isDragging = false;
    let dragStartX = 0;
    let dragStartY = 0;
    let scrollStartLeft = 0;
    let scrollStartTop = 0;

    function setZoom(nextZoom) {
        zoom = Math.min(4, Math.max(1, nextZoom));
        image.style.transform = `scale(${zoom})`;

        if (zoom === 1) {
            modalBody.scrollLeft = 0;
            modalBody.scrollTop = 0;
        }
    }

    zoomButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            const action = button.getAttribute('data-receipt-zoom');

            if (action === 'in') {
                setZoom(zoom + 0.25);
                return;
            }

            if (action === 'out') {
                setZoom(zoom - 0.25);
                return;
            }

            setZoom(1);
        });
    });

    modalBody.addEventListener('mousedown', function (event) {
        if (zoom === 1) {
            return;
        }

        isDragging = true;
        dragStartX = event.clientX;
        dragStartY = event.clientY;
        scrollStartLeft = modalBody.scrollLeft;
        scrollStartTop = modalBody.scrollTop;
        modalBody.classList.add('is-dragging');
    });

    document.addEventListener('mousemove', function (event) {
        if (!isDragging) {
            return;
        }

        modalBody.scrollLeft = scrollStartLeft - (event.clientX - dragStartX);
        modalBody.scrollTop = scrollStartTop - (event.clientY - dragStartY);
    });

    document.addEventListener('mouseup', function () {
        if (!isDragging) {
            return;
        }

        isDragging = false;
        modalBody.classList.remove('is-dragging');
    });

    modal.addEventListener('hidden.bs.modal', function () {
        setZoom(1);
    });
});
