function updateSizeUrl(selectedSize) {
  const url = new URL(window.location);
  const key = "size";

  switch (selectedSize) {
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
  
  window.history.replaceState({}, '', url);

  ResetOtherDropdowns();
}

function ResetOtherDropdowns() {
  var xpath = `//option[contains(text(),'---')]`;
  var matchingElements = getElementsByXPath(xpath);
  if (matchingElements.length === 0) { return; }

  // Change the values and kick off change events
  matchingElements.forEach(element => {
      element.parentNode.value = element.value;
      element.parentNode.dispatchEvent(new Event('change'));
  });
}

function updateBaseUrl(selectedBase) {
  const url = new URL(window.location);
  const key = "base";

  // Use includes to skip the price adjustment information
  if (selectedBase.includes('---')) {
    url.searchParams.delete(key);
  } else if (selectedBase.includes('Low Profile')) {
    url.searchParams.set(key, 'lowprofile');
  } else if (selectedBase.includes('Ergo Extended')) {
    url.searchParams.set(key, 'ergoextended');
  } else if (selectedBase.includes('Ease')) {
    url.searchParams.set(key, 'ease');
  } else if (selectedBase.includes('Ergo')) {
    url.searchParams.set(key, 'ergo');
  } else if (selectedBase.includes('Regular')) {
    url.searchParams.set(key, 'regular');
  }
  
  window.history.replaceState({}, '', url);
}

var aTags = document.getElementsByTagName("dd");

// Changes url to add size when selected
for (var i = 0; i < aTags.length; i++) {
  // map for mattress size
  if (aTags[i].textContent.includes("Twin") ||
      aTags[i].textContent.includes("TwinXL") ||
      aTags[i].textContent.includes("Full") ||
      aTags[i].textContent.includes("Queen") ||
      aTags[i].textContent.includes("King") ||
      aTags[i].textContent.includes("California King")) {
    var sizeSelect = aTags[i].firstElementChild;
    sizeSelect.addEventListener("change", function() {
      updateSizeUrl(sizeSelect.selectedOptions[0].label);
    });
    continue;
  }

  // map for base
  if (aTags[i].textContent.includes("Low Profile") ||
      aTags[i].textContent.includes("Ease") ||
      aTags[i].textContent.includes("Regular") ||
      aTags[i].textContent.includes("Ergo")) {
    var baseSelect = aTags[i].firstElementChild;
    baseSelect.addEventListener("change", function() {
      updateBaseUrl(this.selectedOptions[0].label);
    });
    continue;
  }
}