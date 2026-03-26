document.querySelectorAll('.toggle-pw').forEach(btn => {
    btn.addEventListener('click', function () {
        const id = this.dataset.id;
        document.querySelector(`.pw-mask[data-id="${id}"]`).classList.toggle('d-none');
        document.querySelector(`.pw-hash[data-id="${id}"]`).classList.toggle('d-none');
    });
});
