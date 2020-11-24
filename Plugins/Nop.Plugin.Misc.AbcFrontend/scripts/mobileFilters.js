var homePage = document.documentElement;
var bodyPage = document.getElementsByTagName("body")[0];

function toggleMobileFilters() {
    const isMobileFiltersOpen = $(".nopAjaxFilters7Spikes.open").length === 1;

    if (isMobileFiltersOpen) {
        closeMobileFilters();
    } else {
        openMobileFilters();
    }
}

function openMobileFilters() {
    homePage.classList.add("scrollYRemove");
    bodyPage.classList.add("scrollYRemove");
    $(".nopAjaxFilters7Spikes").addClass("open");

    var overlayCanvas = document.getElementsByClassName("overlayOffCanvas")[0];
    overlayCanvas.classList.add("show");
    overlayCanvas.style.display = "block";

    $(".nopAjaxFilters7Spikes").perfectScrollbar({
        swipePropagation: false,
        wheelSpeed: 1,
        suppressScrollX: true
    });
}

function closeMobileFilters() {
    homePage.classList.remove("scrollYRemove");
    bodyPage.classList.remove("scrollYRemove");
    $(".nopAjaxFilters7Spikes").removeClass("open");

    var overlayCanvas = document.getElementsByClassName("overlayOffCanvas")[0];
    overlayCanvas.classList.remove("show");
    overlayCanvas.style.display = "none";
}