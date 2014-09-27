// Knockout binding for the jquery-expander plugin
define(["knockout", "jquery", "jquery-expander"], function (ko, $) {
    // Add a custom binding for applying the plugin to a DOM element
    ko.bindingHandlers.expander = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Just pass any values through to the plugin
            var val = ko.unwrap(valueAccessor());
            $(element).expander(val);
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        }
    };
});