function mostrarToast() {
    const toastEl = document.getElementById('toast-global');
    const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
    toast.show();
}
