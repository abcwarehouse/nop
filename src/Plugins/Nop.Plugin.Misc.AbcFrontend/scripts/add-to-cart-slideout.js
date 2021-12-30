// Globals
const addToCartSlideoutOverlay = document.getElementById("add-to-cart-slideout-overlay");
const addToCartSlideout = document.getElementById("add-to-cart-slideout");

const input = document.getElementById("add-to-cart-slideout__delivery-zip-code-input");
const checkButton = document.getElementById("add-to-cart-slideout__check-delivery-options");

const deliveryOptions = document.getElementById("add-to-cart-slideout__delivery-options");

// Set up enable/disable for zip code input/button
const zipCodeInput = document.getElementById('add-to-cart-slideout__delivery-zip-code-input');
zipCodeInput.addEventListener('keyup', updateCheckDeliveryAvailabilityButton);

function updateCheckDeliveryAvailabilityButton() {
  
  if (input === undefined) { return; }

  const isNumber = /^\d+$/.test(input.value);

  input.disabled = false;
  checkButton.disabled = !isNumber || input.value.length !== 5;
  checkButton.innerText = "Check Delivery/Pickup Options";
}


function displayAddToCartSlideout(response) {
    document.getElementById("add-to-cart-slideout__product-name").innerText = response.ProductName;
    document.getElementById("add-to-cart-slideout__product-description").innerText = response.ProductDescription;
    document.getElementById("add-to-cart-slideout__product-image").src = response.ProductPictureUrl;
    document.getElementById("add-to-cart-slideout__delivery-input").style.display = response.IsAbcDeliveryItem ? "block" : "none";
    document.getElementById("add-to-cart-slideout__subtotal-value").innerText = response.Subtotal;
    
    showAddToCartSlideout();
}

function showAddToCartSlideout() {
    deliveryOptions.style.display = "none";

    addToCartSlideout.style.width = "320px";
    addToCartSlideout.style.padding = "1rem 1rem 0 1rem";
    addToCartSlideoutOverlay.style.display = "block";
    document.body.classList.add("scrollYRemove");

    updateCheckDeliveryAvailabilityButton();
}

function hideAddToCartSlideout() {
    addToCartSlideout.style.width = "0";
    addToCartSlideout.style.padding = "0";
    addToCartSlideoutOverlay.style.display = "none";
    document.body.classList.remove("scrollYRemove");
}

async function checkDeliveryShippingAvailabilityAsync() {
    input.disabled = true;
    checkButton.disabled = true;
    checkButton.innerText = "Checking...";

    const zip = input.value;
    const response = await fetch(`/AddToCart/GetDeliveryOptions?zip=${zip}`);
    if (response.status != 200) {
        alert('Error occurred when checking delivery options.');
        updateCheckDeliveryAvailabilityButton();
        return;
    }

    const responseJson = await response.json();
    openDeliveryOptions(responseJson);
    updateCheckDeliveryAvailabilityButton();
}

function openDeliveryOptions(response) {
    document.getElementById("add-to-cart-slideout__delivery-options").style.display = "block";
    document.getElementById("add-to-cart-slideout__delivery-input").style.display = "none";
}