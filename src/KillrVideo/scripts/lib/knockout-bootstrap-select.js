// Knockout binding for the bootstrap-select plugin
define(["knockout", "jquery", "bootstrap-select"], function (ko, $) {
    // Add a custom binding for applying the plugin to a DOM element
    ko.bindingHandlers.bootstrapSelect = {
        after: [ "options", "value", "selectedOptions" ],
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Add the selectpicker CSS class to the select element
            if ($(element).hasClass("selectpicker") === false) {
                $(element).addClass("selectpicker");
            }
            
            // Just pass any values through to the jQuery plugin
            var val = ko.unwrap(valueAccessor());
            $(element).selectpicker(val);
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // If certain other bindings change, we'll want to refresh the plugin, so create some dependencies
            allBindings.get("options");
            allBindings.get("selectedOptions");
            allBindings.get("value");
            allBindings.get("enable");
            allBindings.get("disable");

            // Refresh the plugin
            $(element).selectpicker("refresh");
        }
    };
});