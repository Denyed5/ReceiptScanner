document.addEventListener('DOMContentLoaded', function () {
    let cropper = null;

    const cameraInput = document.getElementById('cameraInput');
    const fileInput = document.getElementById('receiptInput');
    const takePhotoBtn = document.getElementById('takePhotoBtn');
    const chooseFileBtn = document.getElementById('chooseFileBtn');
    const image = document.getElementById('image');
    const scanBtn = document.getElementById('scanBtn');
    const form = document.getElementById('uploadForm');
    const emptyPreview = document.getElementById('emptyPreview');
    const croppedImage = document.getElementById('croppedImage');

    if (!cameraInput || !fileInput || !takePhotoBtn || !chooseFileBtn || !image || !scanBtn || !form || !croppedImage) {
        return;
    }

    takePhotoBtn.addEventListener('click', function () {
        cameraInput.click();
    });

    chooseFileBtn.addEventListener('click', function () {
        fileInput.click();
    });

    cameraInput.addEventListener('change', handleImageChange);
    fileInput.addEventListener('change', handleImageChange);

    function handleImageChange(e) {
        const file = e.target.files[0];

        if (!file) {
            return;
        }

        croppedImage.value = '';
        image.src = URL.createObjectURL(file);

        if (emptyPreview) {
            emptyPreview.classList.add('d-none');
        }

        image.onload = function () {
            if (cropper) {
                cropper.destroy();
            }

            cropper = new Cropper(image, {
                viewMode: 1,
                autoCropArea: 1,
                zoomable: false,
                zoomOnWheel: false,
                zoomOnTouch: false,
                responsive: true
            });
        };
    }

    scanBtn.addEventListener('click', function () {
        if (cropper) {
            const canvas = cropper.getCroppedCanvas();
            croppedImage.value = canvas.toDataURL('image/png');
        }

        form.submit();
    });
});