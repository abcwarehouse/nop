function updateSizeUrl(select) {
  const url = new URL(window.location);
  const key = "size";

  switch (select.selectedOptions[0].label) {
    case 'Twin':
      url.searchParams.set(key, 'twin');
      break;
    case 'TwinXL':
      url.searchParams.set(key, 'twinxl');
      break;
    case 'Full':
      url.searchParams.set(key, 'full');
      break;
    case 'Queen':
      url.searchParams.set(key, 'queen');
      break;
    case 'King':
      url.searchParams.set(key, 'king');
      break;
    case 'California King':
      url.searchParams.set(key, 'california');
      break;
  }
  
  window.history.pushState({}, '', url);
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