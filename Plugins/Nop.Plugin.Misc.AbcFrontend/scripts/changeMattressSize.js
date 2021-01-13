function changeMattressSize()
{
    // Get the size value from URL if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const sizeValue = urlParams.get('size');
    if (sizeValue == null || !isValidBase(sizeValue)) { return; }

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

function changeMattressBase()
{
    // Get the base value from URL if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const baseValue = urlParams.get('base');
    if (baseValue == null || !isValidBase(baseValue)) { return; }

    // Find the matching options based on base above (must check all bases)
    var textToFind = getConvertedBase(baseValue);
    var xpath = `//option[contains(text(),'${textToFind}')]`;
    var matchingElements = getElementsByXPath(xpath);
    if (matchingElements.length === 0) { return; }

    // Change the values and kick off change events
    matchingElements.forEach(element => {
        element.parentNode.value = element.value;
        element.parentNode.dispatchEvent(new Event('change'));
    });
}

function isValidBase(size)
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
            return baseValue.toLowerCase().charAt(0).toUpperCase() + baseValue.slice(1);
    }
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

changeMattressSize();
changeMattressBase();