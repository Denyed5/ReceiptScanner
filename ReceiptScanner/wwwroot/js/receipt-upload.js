document.addEventListener('DOMContentLoaded', function () {
    let cropper = null;
    let previewUrl = null;

    const supportedImageTypes = new Set([
        'image/jpeg',
        'image/png',
        'image/bmp'
    ]);
    const supportedImageExtensions = ['.jpg', '.jpeg', '.png', '.bmp'];
    const unsupportedImageMessage = 'Неподдържан файлов формат. Моля добавете с разширение .jpg, .jpeg, .png или .bmp.';

    const cameraInput = document.getElementById('cameraInput');
    const fileInput = document.getElementById('receiptInput');
    const takePhotoBtn = document.getElementById('takePhotoBtn');
    const chooseFileBtn = document.getElementById('chooseFileBtn');
    const image = document.getElementById('image');
    const scanBtn = document.getElementById('scanBtn');
    const form = document.getElementById('uploadForm');
    const emptyPreview = document.getElementById('emptyPreview');
    const croppedImage = document.getElementById('croppedImage');
    const errorModalElement = document.getElementById('uploadErrorModal');
    const errorMessageElement = document.getElementById('uploadErrorMessage');
    const initialErrorMessage = document.getElementById('uploadInitialErrorMessage');

    if (!cameraInput || !fileInput || !takePhotoBtn || !chooseFileBtn || !image || !scanBtn || !form || !croppedImage) {
        return;
    }

    const errorModal = errorModalElement && window.bootstrap
        ? new bootstrap.Modal(errorModalElement)
        : null;

    if (initialErrorMessage && initialErrorMessage.value) {
        showUploadError(initialErrorMessage.value);
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

        if (!isSupportedImage(file)) {
            e.target.value = '';
            resetPreview();
            showUploadError(unsupportedImageMessage);
            return;
        }

        croppedImage.value = '';
        revokePreviewUrl();
        previewUrl = URL.createObjectURL(file);
        image.src = previewUrl;

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

        image.onerror = function () {
            e.target.value = '';
            resetPreview();
            showUploadError('Избраният файл не може да се зареди. Моля изберете друг или пробравайте отново.');
        };
    }

    scanBtn.addEventListener('click', function () {
        if (cropper) {
            const canvas = cropper.getCroppedCanvas();
            croppedImage.value = canvas.toDataURL('image/png');
        }

        const loadingModal = new bootstrap.Modal(
            document.getElementById("loadingModal")
        );
        if (image.src) {
            loadingModal.show();
        }

        form.submit();
    });

    function isSupportedImage(file) {
        const fileName = file.name.toLowerCase();
        const hasSupportedType = file.type && supportedImageTypes.has(file.type.toLowerCase());
        const hasSupportedExtension = supportedImageExtensions.some(function (extension) {
            return fileName.endsWith(extension);
        });

        return hasSupportedType || hasSupportedExtension;
    }

    function resetPreview() {
        croppedImage.value = '';

        if (cropper) {
            cropper.destroy();
            cropper = null;
        }

        image.removeAttribute('src');

        if (emptyPreview) {
            emptyPreview.classList.remove('d-none');
        }

        revokePreviewUrl();
    }

    function revokePreviewUrl() {
        if (previewUrl) {
            URL.revokeObjectURL(previewUrl);
            previewUrl = null;
        }
    }

    function showUploadError(message) {
        if (errorMessageElement) {
            errorMessageElement.textContent = message;
        }

        if (errorModal) {
            errorModal.show();
            return;
        }

        alert(message);
    }
});
