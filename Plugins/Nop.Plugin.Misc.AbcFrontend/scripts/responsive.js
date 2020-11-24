(function ($) {
    $(window).load(function () {
        var catheight = $('.category-navigation-list').height();
        $('.side-2').css('padding-top', catheight - 60);
    });
})(jQuery);