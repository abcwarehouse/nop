function changeMattressSize()
{
    // Get the size value from URL if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const sizeValue = urlParams.get('size');
    if (sizeValue == null || !isValidSize(sizeValue)) { return; }

    // Find the matching option based on size above
    // SPecial case for TwinXL
    var textToFind = sizeValue.toLowerCase() == "twinxl" ?
        "TwinXL" :
        sizeValue.toLowerCase().charAt(0).toUpperCase();
    var xpath = `//option[contains(text(),'${textToFind}')]`;
    var matchingElement = document.evaluate(xpath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
    if (matchingElement == null) { return; }

    // Change the value and kick off change event
    matchingElement.parentNode.value = matchingElement.value;
    matchingElement.parentNode.dispatchEvent(new Event('change'));
}

function isValidSize(size)
{
    const validSizes = ["twin", "twinxl", "full", "queen", "king", "california"];
    return validSizes.includes(size.toLowerCase());
}

changeMattressSize();