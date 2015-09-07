// Binding to allow an element to be used as a bootstrap popover
define(["knockout", "jquery", "bootstrap"], function (ko, $) {
    var resolvedPromise = $.Deferred().resolve().promise();
    var rejectedPromise = $.Deferred().reject().promise();

    function KnockoutPopover(element) {
        this.$element = $(element);
        this.$parent = this.$element.parent();

        this.popoverPromise = resolvedPromise;

        this.visible = false;
        this.config = null;
    }

    // Set the popover configuration (like target, placement, etc)
    KnockoutPopover.prototype.setConfig = function (config) {
        if (config === this.config) return;

        var self = this;
        this.config = config;

        var oldUi = this.popoverPromise;

        this.popoverPromise = this.hide().then(function () {
            return oldUi.then(function($targetEl) {
                // Clean up the old popover element
                if (!$targetEl) return;

                $targetEl.off("show.bs.popover").off("hide.bs.popover");
                $targetEl.popover("destroy");
            });
        }).then(function () {
            // If no configuration, just return null for the target element
            if (!self.config) return null;

            // Create popover
            var $el = $(self.config.target);
            $el.popover({
                html: true,
                placement: self.config.placement,
                title: self.config.title,
                content: self.$element,
                trigger: "manual",
                container: "body"
            });

            if (self.config.onShow) {
                $el.on("show.bs.popover", self.config.onShow);
            }

            if (self.config.onHide) {
                $el.on("hide.bs.popover", self.config.onHide);
            }

            return $el;
        });
    };

    // Show the popover if it's not already visible and return a promise that will be resolved once shown
    KnockoutPopover.prototype.show = function () {
        var self = this;

        return this.popoverPromise.then(function($targetEl) {
            if (!$targetEl) return rejectedPromise;
            if (self.visible === true) return resolvedPromise;

            var defer = $.Deferred();
            $targetEl.one("shown.bs.popover", function () {
                self.visible = true;
                defer.resolve();
            });
            $targetEl.popover("show");
            return defer;
        });
    };

    // Hide the popover if it's visible and return a promise that will be resolved once hidden
    KnockoutPopover.prototype.hide = function () {
        var self = this;

        return this.popoverPromise.then(function($targetEl) {
            if (!$targetEl || self.visible === false) return resolvedPromise;

            var defer = $.Deferred();
            $targetEl.one("hidden.bs.popover", function () {
                self.$element.appendTo(self.$parent);   // Append back to hidden parent to retain events/bindings
                self.visible = false;
                defer.resolve();
            });
            $targetEl.popover("hide");
            return defer;
        });
    };

    // Create the custom KO binding
    ko.bindingHandlers.popover = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var $el = $(element);
            $el.hide();

            // Create the KO popover for managing the UI and store in the data collection
            var $content = $el.children();
            if ($content.length > 1) {
                var container = $("<div></div>").appendTo($el);
                $content.appendTo(container);
                $content = container;
            }
            var koPopover = new KnockoutPopover($content);
            $el.data("koPopover", koPopover);

            // Create a new observable with the appropriate inital value
            var initialValue = ko.unwrap(valueAccessor());
            var observable = ko.observable(initialValue);
            var currentDeferred;

            // Use a computed to track changes to the popover binding's value
            ko.computed(function() {
                if (currentDeferred) {
                    currentDeferred.reject();
                    currentDeferred = null;
                }

                // Subscribe to changes of the popover value and create a deferred that will update the observable created above
                // once the popover has been hidden (to prevent updates to child values while the popover is still visible)
                var newVal = ko.unwrap(valueAccessor());
                currentDeferred = $.Deferred().done(function() {
                    observable(newVal);
                });
                koPopover.hide().done(currentDeferred.resolve);
                koPopover.setConfig(newVal);
            });

            // Give the child elements a new binding context with the popover config as an observable (similar to the 'with' binding)
            var innerBindingContext = bindingContext.createChildContext(observable);
            ko.applyBindingsToDescendants(innerBindingContext, element);

            return { controlsDescendantBindings: true };
        }
    };

    ko.bindingHandlers.popoverVisible = {
        after: ["popover"],
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // We want to make sure show/hide is called whenever the popover binding changes
            var popoverBinding = ko.unwrap(allBindings.get("popover"));

            var val = ko.unwrap(valueAccessor());
            var popover = $(element).data("koPopover");
            if (val) {
                popover.show();
            } else {
                popover.hide();
            }
        }
    };
});