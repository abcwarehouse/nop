$(function () {
    // check if filter applied
    var hash = window.location.hash;

    // if there is not filter applied
    if (!hash || hash.indexOf('noFilterApplied') != -1) {
        // hide normal items
        $('.product-selectors').hide();
        $('.nop7SpikesAjaxFiltersGrid').hide();
        $('.pager').hide();
        $('.featured-product-grid').show();

        // show unfiltered widget
        $('.top-widget-filtered').hide();
        $('.top-widget-unfiltered').show();
    } else {
        // show normal items, hide featured items
        $('.product-selectors').show();
        $('.nop7SpikesAjaxFiltersGrid').show();
        $('.pager').show();
        // hide featured products
        $('.featured-product-grid').hide();

        $('.top-widget-filtered').show();
        $('.top-widget-unfiltered').hide();
    }



    $('.filter-item-name').click(function() {
        //show filtered items
        $('.product-selectors').show();
        $('.nop7SpikesAjaxFiltersGrid').show();
        $('.pager').show();

        // hide featured products
        $('.featured-product-grid').hide();

        // hide current widget, show different widget
        $('.top-widget-filtered').show();
        $('.top-widget-unfiltered').hide();
    });
});
