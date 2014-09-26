define(["knockout", "jquery", "perfect-scrollbar"], function (ko, $) {
    // Add a custom binding for applying the plugin to a DOM element
    ko.bindingHandlers.perfectScrollbar = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Just pass any values through to the jQuery slim scroll plugin
            var val = ko.unwrap(valueAccessor());
            $(element).perfectScrollbar(val);

            // The scroll bar doesn't always render initally since the div it's attached to is still processing, so attempt
            // to "refresh" it a second after this is initialized
            setTimeout(function() {
                $(element).perfectScrollbar('update');
            }, 1000);
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        }
    };
});