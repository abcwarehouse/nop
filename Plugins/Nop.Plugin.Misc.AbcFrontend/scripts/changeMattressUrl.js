function updateSizeUrl(select) {
  const urlParams = new URLSearchParams(window.location.search);
  const key = "size";

  switch (select.selectedOptions[0].label) {
    case 'Twin':
      urlParams.set(key, "twin");
      break;
    case 'TwinXL':
      urlParams.set(key, "twinxl");
      break;
    case 'Full':
      urlParams.set(key, "full");
      break;
    case 'Queen':
      urlParams.set(key, "queen");
      break;
    case 'King':
      urlParams.set(key, "king");
      break;
    case 'California King':
      urlParams.set(key, "california");
      break;
  }
  var newUrl = urlParams.toString()
  window.history.pushState({path: newUrl}, '', newUrl);
}

var aTags = document.getElementsByTagName("dd");

// Changes url to add size when selected
for (var i = 0; i < aTags.length; i++) {
  if (aTags[i].textContent.includes("Twin") ||
      aTags[i].textContent.includes("TwinXL") ||
      aTags[i].textContent.includes("Full") ||
      aTags[i].textContent.includes("Queen") ||
      aTags[i].textContent.includes("King") ||
      aTags[i].textContent.includes("California King")) {
    var select = aTags[i].firstElementChild;
    select.addEventListener("change", function() {
      updateSizeUrl(select);
    });
    break;
  }
}