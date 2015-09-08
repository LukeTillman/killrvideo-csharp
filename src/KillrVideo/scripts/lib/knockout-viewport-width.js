define(["knockout"], function(ko) {
    // Custom KO binding that reports the viewport width
    ko.bindingHandlers.viewportWidth = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var val = valueAccessor();
            val(window.innerWidth);

            // TODO: Could react to window resize (throttled) and update value
        }
    };

    // Allow use on virtual elements
    ko.virtualElements.allowedBindings.viewportWidth = true;
});

