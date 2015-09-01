// Knockout binding to format and set an element's text using the moment plugin
define(["knockout", "moment"], function (ko, moment) {
    // Add a custom binding for applying the plugin to a DOM element
    ko.bindingHandlers.momentText = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            return { 'controlsDescendantBindings': true };
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Val should be something like "{ text: someValueOrObservable, format: someValueOrObservable }"
            var val = ko.unwrap(valueAccessor());
            if (val !== null && typeof val === "object") {
                var dateString = moment(ko.unwrap(val.text)).format(ko.unwrap(val.format));
                ko.utils.setTextContent(element, dateString);
            }
        }
    };
});