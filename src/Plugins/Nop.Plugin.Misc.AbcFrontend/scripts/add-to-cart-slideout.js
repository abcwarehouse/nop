function showAddToCartSlideout() {
    document.getElementById("add-to-cart-slideout").style.width = "320px";
    document.getElementById("add-to-cart-slideout").style.padding = "1rem 1rem 0 1rem";
    document.getElementById("add-to-cart-slideout-overlay").style.display = "block";
    document.body.classList.add("scrollYremove");
}

function hideAddToCartSlideout() {
    document.getElementById("add-to-cart-slideout").style.width = "0";
    document.getElementById("add-to-cart-slideout").style.padding = "0";
    document.getElementById("add-to-cart-slideout-overlay").style.display = "none";
    document.body.classList.remove("scrollYRemove");
}

function checkDeliveryShippingAvailability() {
    alert('test');
}