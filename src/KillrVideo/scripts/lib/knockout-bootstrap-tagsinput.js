// Knockout binding for the bootstrap-tagsinput plugin
define(["knockout", "jquery", "bootstrap-tagsinput"], function (ko, $) {
    // Add a custom binding for applying the plugin to a DOM element
    ko.bindingHandlers.bootstrapTagsInput = {
        after: ["selectedOptions", "value"],
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Use value as options
            var val = ko.unwrap(valueAccessor());

            // Create the plugin
            var $el = $(element);
            $el.tagsinput(val);

            // Get the element added by the plugin
            var $pluginDiv = $(element).siblings("div.bootstrap-tagsinput").first();

            // Transfer any classes on the original element to the element inserted by the plugin
            var classesOnOriginalElement = $el.attr("class");
            if (classesOnOriginalElement) {
                $pluginDiv.removeClass("bootstrap-tagsinput")
                    .addClass(classesOnOriginalElement + " bootstrap-tagsinput");
            }

            // Some hacks to overcome some UI quirks (that may be fixed in the underlying plugin eventually)
            $pluginDiv.children("input[type=text]").first().css("width", "");
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        }
    };
});