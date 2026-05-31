function openDeleteModal(id, type) {

    const hiddenInput = document.getElementById("deleteEntityId");
    const form = document.getElementById("deleteForm");

    if (!hiddenInput || !form) {
        console.error("Modal not found in DOM");
        return;
    }

    hiddenInput.value = id;

    type = type.trim().charAt(0).toUpperCase() + type.trim().slice(1).toLowerCase();

    form.action = `/_Actions?handler=Delete${type}`;

    document.getElementById("deleteModalTitle").innerText = `Delete ${type}?`;

    document.getElementById("deleteModalText").innerText =
        `Are you sure you want to delete this ${type.toLowerCase()}?`;

    const modal = document.getElementById("deleteModal");

    document.activeElement.blur();

    modal.style.display = "flex";

    setTimeout(() => {

        const textarea = modal.querySelector("textarea");

        if (textarea) {
            textarea.focus();
        }

    }, 100);
}

function closeDeleteModal() {

    document.getElementById("deleteModal").style.display = "none";
}


function openReportModal(id, type) {

    const hiddenInput = document.getElementById("reportEntityId");
    const hiddenType = document.getElementById("reportEntityType");

    if (!hiddenInput || !hiddenType) {
        console.error("Report modal not found in DOM");
        return;
    }

    hiddenInput.value = id;
    hiddenType.value = type;

    document.getElementById("reportModalTitle").innerText =
        `Report ${type}`;

    document.getElementById("reportModalText").innerText =
        `Are you sure you want to report this ${type.toLowerCase()}?`;

    const modal = document.getElementById("reportModal");

    document.activeElement.blur();

    modal.style.display = "flex";

    setTimeout(() => {

        const textarea = modal.querySelector("textarea");

        if (textarea) {
            textarea.focus();
        }

    }, 100);
}

function closeReportModal() {

    document.getElementById("reportModal").style.display = "none";
}