// Currency formatter
var formatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD'
});

// Globals
const addToCartSlideoutOverlay = document.getElementById("add-to-cart-slideout-overlay");
const addToCartSlideout = document.getElementById("add-to-cart-slideout");
const addToCartSlideoutBackButton = document.getElementById("add-to-cart-slideout__back");

const input = document.getElementById("add-to-cart-slideout__delivery-input");
const zipCodeInput = document.getElementById('add-to-cart-slideout__delivery-zip-code-input');
const checkButton = document.getElementById("add-to-cart-slideout__check-delivery-options");

const deliveryNotAvailable = document.getElementById("add-to-cart-slideout__delivery-not-available");
const deliveryOptions = document.getElementById("add-to-cart-slideout__delivery-options");


// Set up enable/disable for zip code input/button
zipCodeInput.addEventListener('keyup', updateCheckDeliveryAvailabilityButton);

function updateCheckDeliveryAvailabilityButton() {
  if (zipCodeInput === undefined) { return; }

  const isNumber = /^\d+$/.test(zipCodeInput.value);

  zipCodeInput.disabled = false;
  checkButton.disabled = !isNumber || zipCodeInput.value.length !== 5;
  checkButton.innerText = "Check Delivery/Pickup Options";
}


function displayAddToCartSlideout(response) {
    document.getElementById("add-to-cart-slideout__delivery-input").style.display = response.IsAbcDeliveryItem ? "block" : "none";
    document.getElementById("add-to-cart-slideout__subtotal-value").innerText = formatter.format(response.Subtotal);
    
    showAddToCartSlideout();
}

function showAddToCartSlideout() {
    deliveryOptions.style.display = "none";
    deliveryNotAvailable.style.display = "none";
    addToCartSlideoutBackButton.style.display = "none";

    addToCartSlideout.style.width = "320px";
    addToCartSlideout.style.padding = "2.5rem 1rem 0 1rem";
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
    zipCodeInput.disabled = true;
    checkButton.disabled = true;
    checkButton.innerText = "Checking...";

    const zip = zipCodeInput.value;
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
    document.getElementById("add-to-cart-slideout__delivery-input").style.display = "none";
    
    deliveryNotAvailable.style.display = "none";
    deliveryOptions.style.display = "none";

    if (response.isDeliveryAvailable) {
        deliveryOptions.style.display = "block";
    } else {
        deliveryNotAvailable.style.display = "block";
    }

    addToCartSlideoutBackButton.style.display = "block";
}

function back() {
    deliveryNotAvailable.style.display = "none";
    deliveryOptions.style.display = "none";
    addToCartSlideoutBackButton.style.display = "none";

    input.style.display = "block";
}