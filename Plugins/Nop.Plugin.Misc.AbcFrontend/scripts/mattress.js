// ------
// onLoad
// ------
function changeMattressSize()
{
    // Get the size value from URL if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const sizeValue = urlParams.get('size');
    if (sizeValue == null || !isValidSize(sizeValue)) { return; }

    // Find the matching option based on size above
    // Special case for TwinXL
    var textToFind = getConvertedSize(sizeValue);
    var mattressSizeSelect = document.getElementsByClassName("mattress-size");
    if (mattressSizeSelect.length != 1) { return; }

    for (var i = 0; i < mattressSizeSelect[0].options.length; i++) {
        if (mattressSizeSelect[0].options[i].text === textToFind) {
            mattressSizeSelect[0].selectedIndex = i;
            mattressSizeSelect[0].dispatchEvent(new Event('change'));
            break;
        }
    }
}

function getConvertedSize(sizeValue) {
  switch (sizeValue) {
      case 'twinxl':
          return "TwinXL";
      case 'california':
              return "California King";
      default:
          return `${sizeValue.toLowerCase().charAt(0).toUpperCase()}${sizeValue.toLowerCase().substring(1)}`;
  }
} 

function changeMattressBase()
{
    // Get the base value from URL if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const baseValue = urlParams.get('base');
    if (baseValue == null || !isValidBase(baseValue)) { return; }

    // Find the matching options based on base above (must check all bases)
    var textToFind = getConvertedBase(baseValue);
    var mattressBaseSelects = document.getElementsByClassName("mattress-base");
    if (mattressBaseSelects.length <= 0) { return; }

    for (var i = 0; i < mattressBaseSelects.length; i++)
    {
        for (var j = 0; j < mattressBaseSelects[i].options.length; j++) {
            if (mattressBaseSelects[i].options[j].text.includes(textToFind)) {
                mattressBaseSelects[i].selectedIndex = j;
                mattressBaseSelects[i].dispatchEvent(new Event('change'));
                break;
            }
        }
    }
}

function isValidSize(size)
{
    const validSizes = ["twin", "twinxl", "full", "queen", "king", "california"];
    return validSizes.includes(size.toLowerCase());
}

function isValidBase(base)
{
    const validBases = ["lowprofile", "ease", "regular", "ergo", "ergoextended"];
    return validBases.includes(base.toLowerCase());
}

function getConvertedBase(baseValue) {
    switch (baseValue) {
        case 'lowprofile':
            return "Low Profile";
        case 'ergoextended':
                return "Ergo Extended";
        default:
            return `${baseValue.toLowerCase().charAt(0).toUpperCase()}${baseValue.toLowerCase().substring(1)}`;
    }
}

changeMattressSize();
changeMattressBase();

// --------
// onChange
// --------
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
    default:
      throw new Error('Unable to match mattress size, cannot update URL.');
  }
  
  window.history.replaceState({}, '', url);
  ResetOtherDropdowns();
}

function getElementsByXPath(xpath) {
  let results = [];
  let query = document.evaluate(xpath, document,
      null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
  for (let i = 0, length = query.snapshotLength; i < length; ++i) {
      results.push(query.snapshotItem(i));
  }
  return results;
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

// Adds event listeners to changes for sizes and bases
var mattressSizeSelect = document.getElementsByClassName("mattress-size");
if (mattressSizeSelect.length == 1) {
  mattressSizeSelect[0].addEventListener("change", function() {
    updateSizeUrl(mattressSizeSelect[0].selectedOptions[0].label);
  });
}

var mattressBaseSelects = document.getElementsByClassName("mattress-base");
if (mattressBaseSelects.length > 0) {
  for (var i = 0; i < mattressBaseSelects.length; i++) {
    mattressBaseSelects[i].addEventListener("change", function() {
      updateBaseUrl(this.selectedOptions[0].label);
    });
  }
}