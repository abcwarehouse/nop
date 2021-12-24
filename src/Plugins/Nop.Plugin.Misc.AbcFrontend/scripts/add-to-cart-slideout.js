function closeAddToCartSlideout() {
    document.getElementById("add-to-cart-slideout").style.width = "0";
    document.getElementById("add-to-cart-slideout__overlay").style.display = "none";
    document.body.classList.remove("scrollYRemove");
}