(function ($) {

    $(document).ready(function () {

        var dependencies = [
            {
                module: "header",
                dependencies: ["attachDetach", "overlay", "perfectScrollbar"]
            },
            {
                module: "menu",
                dependencies: ["perfectScrollbar"]
            }
        ];

        var themeSettings = {

            lazyLoadProductBoxImages: {
                selector: '.item-box .picture a img[data-lazyloadsrc]'
            },
            goToTop: {
                scrollElementSelector: '#goToTop'
            },
            overlay: {
                overlayElementSelector: '.overlayOffCanvas',
                overlayClass: 'show',
                noPageScrollClass: 'scrollYRemove'
            },
            productQuantity: {
                quantityInputSelector: '.qty-input, .productQuantityTextBox',
                incrementSelectors: '.plus',
                decrementSelectors: '.minus'
            },
            flyoutCart: {
                flyoutCartSelector: '.flyout-cart-wrapper'
            },
            attachDetach: {
                blocks: [
                    {
                        content: '.one-column-blocks-wrapper #PollList',
                        elementToAttach: '.poll-list-wrapper'
                    },
                    {
                        content: '.blog-search-box',
                        elementToAttach: '.side-2',
                        insertAction: 'prependTo'
                    },
                    {
                        content: '.blogpost-page .post-footer',
                        elementToAttach: '.blogpost-page .page-body',
                        insertAction: 'appendTo'
                    },
                    {
                        content: '.one-column .filter-block .toggleControl',
                        elementToAttach: '.filtersPanelNav .title'
                    }
                ]
            },
            toggle: {
                blocks: [
                    {
                        opener: '.search-box-opener',
                        content: '.store-search-box form',
                        activeClassOpener: 'open',
                        activeClassContent: 'open',
                        animation: {
                            type: 'none'
                        }
                    }, 
                    {
                        opener: '.coupon-box-wrapper > .title',
                        content: '.coupon-box',
                        activeClassOpener: 'open',
                        animation: {
                            type: 'slide',
                            speed: 'slow'
                        }
                    },
                    {
                        opener: '.giftcard-box-wrapper > .title',
                        content: '.giftcard-box',
                        activeClassOpener: 'open',
                        animation: {
                            type: 'slide',
                            speed: 'slow'
                        }
                    },
                    {
                        opener: '.shipping-wrapper .title',
                        content: '.shipping',
                        activeClassOpener: 'open',
                        animation: {
                            type: 'slide',
                            speed: 'slow'
                        }
                    } 
                ]
            },
            equalizer: {
                blocks: [
                    {
                        selector: '.item-box .product-title'
                    },
                    {
                        selector: '.item-box .prices'
                    },
                    {
                        selector: '.news-list-homepage .news-item'
                    },
                    {
                        selector: '.featured-product-grid .item-box'
                    },
                    {
                        selector: '.request-item'
                    },
                    {
                        selector: '.checkout-page .address-box'
                    },
                    {
                        selector: '.checkout-data .method-list li'
                    },
                    {
                        selector: '.return-request-page form > div'
                    },
                    {
                        selector: '.news-list-page .news-item'
                    },
                    {
                        selector: '.blog-page .post-title'
                    },
                    {
                        selector: '.blog-page .blog-details'
                    }
                ],
                triggerEvents: 'nopAjaxFiltersFiltrationCompleteEvent'
            },
            responsive: [
                {
                    breakpoint: 1261,
                    settings: {
                        menu: {
                            sublistIndent: {
                                enabled: true,
                                topLevelMenuWidth: 285
                            }
                        },
                        header: {
                            activeClass: 'open',
                            modules: [
                                {
                                    opener: '.menu-open',
                                    closer: '.menu-close',
                                    content: '.header-menu',
                                    overlay: true, disablePageScroll: true,
                                    scrollbar: '' //disabled, use default scroll
                                },
                                {
                                    opener: '.mobile-flyout-cart-toggle',                                    
                                    content: '.header-flyout-cart',
                                    overlay: true, disablePageScroll: true,
                                    scrollbar: '' //disabled, use default scroll
                                },
                                {
                                    opener: '.filters-button',
                                    closer: '.close-filters',
                                    content: '.nopAjaxFilters7Spikes',
                                    overlay: true, disablePageScroll: true,
                                    scrollbar: '' //disabled, use default scroll
                                }
                            ]
                        },
                        attachDetach: {
                            blocks: [
                                {
                                    content: '.store-search-box',
                                    elementToAttach: '.responsive-strip'
                                },
                                {
                                    content: '.product-details-page .product-name',
                                    elementToAttach: '.product-details-top',
                                    insertAction: 'prependTo'
                                },
                                {
                                    content: '.product-social-buttons',
                                    elementToAttach: '.overview'
                                },
                                {
                                    content: '.product-page-layout-two .ui-tabs.productTabs',
                                    elementToAttach: '.product-page-layout-two .product-essential'
                                },
                                {
                                    content: '.product-page-layout-two .product-collateral',
                                    elementToAttach: '.product-page-layout-two .product-essential'
                                },
                                {
                                    content: '.blogpost-page .post-footer',
                                    elementToAttach: '.blogpost-page .page-body',
                                    insertAction: 'appendTo'
                                }
                            ]
                            
                        },
                        toggle: {
                            blocks: [
                                {
                                    opener: '.search-box-opener',
                                    content: '.store-search-box form',
                                    activeClassOpener: 'open',
                                    activeClassContent: 'open',
                                    animation: {
                                        type: 'none'
                                    }
                                },
                                {
                                    opener: '.footer-block .title',
                                    content: '.footer-collapse',
                                    activeClassOpener: 'open',
                                    animation: {
                                        type: 'slide',
                                        speed: 'slow'
                                    }
                                },
                                {
                                    opener: '.block .title',
                                    content: '.listbox',
                                    activeClassOpener: 'open',
                                    animation: {
                                        type: 'slide',
                                        speed: 'slow'
                                    }
                                },
                                {
                                    opener: '.coupon-box-wrapper .title',
                                    content: '.coupon-box',
                                    activeClassOpener: 'open',
                                    animation: {
                                        type: 'slide',
                                        speed: 'slow'
                                    }
                                },
                                {
                                    opener: '.giftcard-box-wrapper .title',
                                    content: '.giftcard-box',
                                    activeClassOpener: 'open',
                                    animation: {
                                        type: 'slide',
                                        speed: 'slow'
                                    }
                                },
                                {
                                    opener: '.shipping-wrapper .title',
                                    content: '.shipping',
                                    activeClassOpener: 'open',
                                    animation: {
                                        type: 'slide',
                                        speed: 'slow'
                                    }
                                }
                            ]
                        },
                        equalizer: {
                            blocks: [
                                {
                                    selector: '.item-box .product-title'
                                },
                                {
                                    selector: '.item-box .prices'
                                },
                                {
                                    selector: '.news-list-homepage .news-item'
                                },
                                {
                                    selector: '.product-list .product-item > div'
                                },
                                {
                                    selector: '.cart tr'
                                },
                                {
                                    selector: '.request-item'
                                },
                                {
                                    selector: '.checkout-page .address-box'
                                },
                                {
                                    selector: '.checkout-data .method-list li'
                                },
                                {
                                    selector: '.news-list-page .news-item'
                                },
                                {
                                    selector: '.blog-page .post-title'
                                },
                                {
                                    selector: '.blog-page .blog-details'
                                }
                            ]
                        },
                        filters: {
                            closePanelAfterFiltrationDataAttribute: 'closefilterspanelafterfiltrationinmobile'
                        }
                    }
                }
            ]
        };

        var theme = new sevenSpikesTheme(themeSettings, dependencies, false);

        theme.init();

        // invoke functions - N

        productPagePictureSlider();
        triggerCategoryPageTabs();
        triggerLoginPageTabs();
        triggerGdprPageTabs();

        // invoke functions - X

        handleMenuSearchOverlap();
        handleHeaderMenuWrapping();
        handleQuantityAttributes();
        handleContactPagesTextaArea();
        createTabsForAjaxFilters();
        handleMegaMenuSubcategories();
        handleHomePageRichBlogClasses();
        handleHomePageRichBlogLayout();
        handleSaleOfTheDayNavigationLayout();
        replaceDefaultSaleOfTheDayNavigation();

        if (!$('.ropc').length > 0) {
            handleFormsWithErrors(".coupon-box .message-failure, .coupon-box .message-success", ".coupon-box-wrapper > .title");
            handleFormsWithErrors(".giftcard-box .message-failure, .giftcard-box .message-success", ".giftcard-box-wrapper > .title");
        }
        handleFormsWithErrors(".edit-address .field-validation-error", ".edit-address .input-validation-error");
        handleFormsWithErrors(".new-comment .field-validation-error", ".new-comment textarea");

    }); // document.ready



    // custom slick slider for product details gallery - N

    var isCarouselRtl = $('#pacificIsRtl').val() === 'true' 

    var slickSettings = {
        rtl: isCarouselRtl,
        arrows: false,
        dots: true,
        infinite: true,
        mobileFirst: true,
        adaptiveHeight: true,
        slidesToShow: 1,
        slidesToScroll: 1,
        breakpoint: reSlickSlider,
        responsive: [
            {
                breakpoint: 1260,
                settings: "unslick"
            },
            {
                breakpoint: 9999,
                setting: {}
            }
        ]
    };
    function reSlickSlider() {
        var windowWidth = $(window).width();

        if (windowWidth < 1260) {

            var sliderContainer = $('.product-page-layout-two .all-pictures');

            sliderContainer.slick('unslick');
            sliderContainer.slick(slickSettings);
        }
    }
    $(window).on('resize', reSlickSlider);

    function productPagePictureSlider() {
        $('.product-page-layout-two .all-pictures').slick(slickSettings);
    }


    // searchbar scroll behavior (mobile only) - N


    var lastScrollTop = 0;
      
    function hasScrolled() {

        var windowScrollTop = $(window).scrollTop();

        if (windowScrollTop > 50 && windowScrollTop > lastScrollTop) {
            $('.store-search-box').addClass('nav-down');
            $('.scroll-back-button').removeClass('nav-up');
            $('.admin-header-links').removeClass('nav-up');
        }
        else {
            $('.store-search-box').removeClass('nav-down');
            $('.scroll-back-button').addClass('nav-up');
            $('.admin-header-links').addClass('nav-up');
        }

        lastScrollTop = windowScrollTop;
    }
    $(window).on('scroll.theme.pacific.custom.search', hasScrolled);



    // custom UI tabs on catalog and login pages
    

    function triggerCategoryPageTabs() {
        $(".side-navigation-tabs").tabs();
    }

    function triggerLoginPageTabs() {
        $(".login-tabs").tabs();
    }

    function triggerGdprPageTabs() {
        $(".gdpr-tabs").tabs();
    }





    //   N E W  C U S T O M  S E T T I N G S   -   X





    // additional dynamic classname to resolve HeaderMenu & Search overlap conflict
    function handleMenuSearchOverlap() {
        var headerMenu = $('.header-menu');

        $('.search-box-opener').on('click', function () {

            if (!$(this).hasClass('open')) {
                headerMenu.addClass('disable');
            }
            else {
                setTimeout(function () {
                    headerMenu.removeClass('disable');
                }, 500); // time should match the css transition delay time
            }
        });
        $(document).on('themeBreakpoint', function (event, settings, breakpoint) {
            headerMenu.removeClass('disable');
        });
    } // invoke on document.ready




    // additional classname when header menu items wrap on more than 1 line
    function handleHeaderMenuWrapping() {
        var headerMenu = $('.header-menu');
        var menuItems = $('.top-menu > li, .mega-menu > li');
        var menuItemsOffset = menuItems.last().position().top;

        if (menuItemsOffset > 25) {
            headerMenu.addClass('wrapped');
        }
        else {
            headerMenu.removeClass('wrapped');
        }
    } // invoke on document.ready
    $(window).on('resize', handleHeaderMenuWrapping);




    // additional classname for quantity attributes
    function handleQuantityAttributes() {
        var targetElement = $('.attributes .qty-box');
        if (targetElement.length > 0) {
            targetElement.prev().addClass('qty-box-trigger');
            targetElement.parent().addClass('qty-box-parent');
        }
    } // invoke on document.ready
    $(document).on('nopAjaxCartMiniProductDetailsViewShown', handleQuantityAttributes);




    // additional class name for textarea input line on contact pages
    function handleContactPagesTextaArea() {
        $('.contact-page .inputs').last().addClass('last');
        $('.contact-page .inputs').last().addClass('last');
        $('.contact-page .inputs').last().addClass('last');
    } // invoke on document.ready




    // equalize height for banner boxes inside the featured products grid
    function equalizeBannerBoxHeight() {
        var bannerBox = $('.home-page-product-grid .banner-box');
        var targetHeight = bannerBox.next('.item-box').outerHeight();
        bannerBox.height(targetHeight);
    }
    $(window).on('equalizerExecuted', equalizeBannerBoxHeight);




    // emulate tabs for AjaxFilters on one-column catalog pages
    function createTabsForAjaxFilters() {

        $('.one-column .filter-block.selected-options').remove();

        var filterBlocks = $('.one-column .filtersPanel .filter-block');
        filterBlocks.first().addClass('active');

        $('.one-column .toggleControl').first().addClass('active');

        $('.filtersPanelNav').on('click', '.toggleControl', function () {

            $(this).addClass('active').siblings().removeClass('active');

            var tabIndex = $(this).index();
            filterBlocks.removeClass('active');
            filterBlocks.eq(tabIndex).addClass('active');
        });
    } // invoke on document.ready




    // workaround for equalizing heights in jCarousel items, Equalizer script is not working with cloned items
    function equalizeCarouselItemsHeight() {
        $('.nop-jcarousel.product-grid').each(function () {
            var titleElement = $(this).find('.item-box .product-title');
            var pricesElement = $(this).find('.item-box .prices');

            titleElement.removeAttr('style');
            pricesElement.removeAttr('style');

            var titleHeight = 0;
            var pricesHeight = 0;

            titleElement.each(function () {
                var originalHeight = $(this).outerHeight();

                if (originalHeight > titleHeight) {
                    titleHeight = originalHeight;
                }
            });
            titleElement.height(titleHeight);

            pricesElement.each(function () {
                var originalHeight = $(this).outerHeight();

                if (originalHeight > pricesHeight) {
                    pricesHeight = originalHeight;
                }
            });
            pricesElement.height(pricesHeight);

        });
    }
    $(window).on('jCarouselProductsAddedToPageEvent', equalizeCarouselItemsHeight);




    // additional classname when MegaMenu dropdown has any subcategories
    function handleMegaMenuSubcategories() {
        if ($('.mega-menu .subcategory-item').length > 0) {
            $('.mega-menu .box .wrapper').addClass('full');
        }
    } // invoke on document.ready
    



    // additional classnames to handle the Rich Blog on homepage
    function handleHomePageRichBlogClasses() {
        var blogPosts = $('.rich-blog-homepage .blog-posts');

        blogPosts.children('.blog-post').first().addClass('first');

        if (blogPosts.children('.blog-post').length == 1) {
            blogPosts.addClass('single-post');
        }
        else if (blogPosts.children('.blog-post').length == 2) {
            blogPosts.addClass('two-posts');
            blogPosts.children('.blog-post').addClass('first');
        }
        else if (blogPosts.children('.blog-post').length == 3) {
            blogPosts.addClass('three-posts');
            blogPosts.children('.blog-post').addClass('first');
        }
    } // invoke on document.ready
    



    // equalize heights in the Rich Blog layout on homepage
    function handleHomePageRichBlogLayout() {
        var targetHeight = $('.rich-blog-homepage .right-side').outerHeight();
        var targetElement = $('.rich-blog-homepage .left-side .post-details');
        var elementHeight = targetHeight - targetElement.prev('.post-picture').outerHeight();

        targetElement.css('min-height', elementHeight -2);
    } // invoke on document.ready

	$(document).on('themeBreakpoint', function (event, settings, breakpoint) {
		if(breakpoint === null) {
            setTimeout(function () {
                handleHomePageRichBlogLayout();
            }, 100);
		}
    }); $(window).on('resize', handleHomePageRichBlogLayout);




    // additional class for Sale Of The Day title when carousel is initialized
    function handleSaleOfTheDayNavigationLayout() {
        var mainContainer = $('.sale-of-the-day-offer');

        if (mainContainer.children('.grid').hasClass('owl-carousel')) {
            mainContainer.children('.title').addClass('has-navigation');
        }
    } // invoke on document.ready
    



    // emulation of default "Sale of the Day" navigation
    function replaceDefaultSaleOfTheDayNavigation() {
        var targetArea = $('.sale-of-the-day-offer');

        targetArea.find('.new-prev').on('click', function () {
            targetArea.find('.owl-prev').click();
        });
        targetArea.find('.new-next').on('click', function () {
            targetArea.find('.owl-next').click();
        });
    } // invoke on document.ready




    // equilize layout heights for Smart Product & Category Collections

    function equalizeSpcProductsHeight() {
        $('.spc-products .product-grid.active').each(function () {
            var titleElement = $(this).find('.item-box .product-title');
            var pricesElement = $(this).find('.item-box .prices');

            var titleHeight = 0;
            var pricesHeight = 0;

            titleElement.each(function () {
                var originalHeight = $(this).outerHeight();

                if (originalHeight > titleHeight) {
                    titleHeight = originalHeight;
                }
            });
            titleElement.height(titleHeight);

            pricesElement.each(function () {
                var originalHeight = $(this).outerHeight();

                if (originalHeight > pricesHeight) {
                    pricesHeight= originalHeight;
                }
            });
            pricesElement.height(pricesHeight);
        });
    }

    function equalizeSpcCategoriesHeight() {
        $('.spc-categories .category-product-grid.active').each(function () {

            $(this).find('.item-box:nth-child(odd)').each(function () {
                var oddHeight = $(this).find('.prices').outerHeight();
                var evenHeight = $(this).next().find('.prices').outerHeight();

                if (oddHeight == evenHeight) {
                    return;
                }
                else if (oddHeight > evenHeight) {
                    evenHeight = oddHeight;
                }
                else if (oddHeight < evenHeight) {
                    oddHeight = evenHeight;
                }

                $(this).find('.prices').height(oddHeight);
                $(this).next().find('.prices').height(evenHeight);
            });

            var numberOfBoxes = $(this).find('.item-box').length;

            if (numberOfBoxes > 4) {
                $(this).find('.item-grid').addClass('full');
            }
        });
    }

    $(window).on('newProductsAddedToPageEvent', equalizeSpcProductsHeight);
    $(window).on('newProductsAddedToPageEvent', equalizeSpcCategoriesHeight);

    function fixSpcDynamicHeightIssues() {
        var windowResized = false;

        $(window).on('resize', function () {
            clearTimeout(window.resizeFinished);
            window.resizeFinished = setTimeout(function () {
                $('.spc .active').siblings().find('.product-title, .prices').removeAttr('style');
            }, 100);
            windowResized = true;
        });

        $('.spc-products').on('click', '.navigation .tab', function () {
            if (windowResized) {
                setTimeout(equalizeSpcProductsHeight, 15);
            }
        });
        $('.spc-categories').on('click', '.navigation .tab', function () {
            if (windowResized) {
                setTimeout(equalizeSpcCategoriesHeight, 15);
            }
        });
    }
    fixSpcDynamicHeightIssues();




    // forms will expand automatically if there is error/success notification
    function handleFormsWithErrors(errors, opener) {

        if ($(errors).length > 0) {
            $(opener).click();
            $('html, body').animate({
                scrollTop: $(opener).offset().top
            }, 1000);
        }
    } // invoke on document.ready

})(jQuery);