// Custom KO binding for enabling tour popovers with bootstrap
define(["knockout", "jquery", "bootstrap"], function (ko, $) {
    function TourUi(element) {
        this.$element = $(element);
        this.visible = false;
        this.currentStep = null;
        this.beforeShowDeferred = $.Deferred().reject();
        this.uiElementPromise = $.Deferred().reject().promise();

        this.$tourContentWrapper = $(
            "<div class='tour small'>" +
            "  <div class='tour-content'></div>" +
            "  <div class='tour-buttons clearfix'>" +
            "    <button class='btn btn-sm btn-default tour-buttons-previous'><i class='fa fa-chevron-circle-left'></i> Back</button>" +
            "    <div class='pull-right'>" +
            "      <button class='btn btn-sm btn-primary tour-buttons-next'>Next <i class='fa fa-chevron-circle-right'></i></button>" +
            "    </div>" +
            "  </div>" +
            "</div>"
        ).on("click", "button", $.proxy(this.handleTourClick, this));

        this.$nextButton = this.$tourContentWrapper.find(".tour-buttons-next");
        this.$previousButton = this.$tourContentWrapper.find(".tour-buttons-previous");
        this.$tourContent = this.$tourContentWrapper.children(".tour-content");
    };

    // Set the current step
    TourUi.prototype.setStep = function(step) {
        if (this.currentStep === step) return;

        // Remember whether we're currently visible
        var isVisible = this.visible;

        var self = this;

        // Reject before show deferred from previous step (no op if already resolved)
        this.beforeShowDeferred.reject();

        // Remove any previous tour click handlers
        $("body").off("click.tour");

        // Create a promise that the old UI will be torn down
        var tearDownOldUi = $.Deferred();
        this.uiElementPromise.done(function($el) {
            // Resolve once the UI has been hidden by the destroy call
            $el.one("hidden.bs.popover", function () {
                $el.popover("destroy");
                self.visible = false;
                tearDownOldUi.resolve();
            });
            $el.popover("hide");    // TODO: If upgrade to BS 3.3 can just destroy here
        }).fail(function() {
            // UI was never shown so just resolve
            tearDownOldUi.resolve();
        });
        
        // Transition to next step
        this.currentStep = step;
        this.beforeShowDeferred = null;
        
        // Create a promise for creating the UI
        this.uiElementPromise = tearDownOldUi
            .then(function() {
                // Store a deferred that can be cancelled to wait on the before show promise for the current step
                self.beforeShowDeferred = $.Deferred();
                if (self.currentStep.beforeShowPromise) {
                    self.currentStep.beforeShowPromise().done(function() {
                        self.beforeShowDeferred.resolve();
                    });
                } else {
                    self.beforeShowDeferred.resolve();
                }
                return self.beforeShowDeferred;
            })
            .then(function () {
                // Attach next click handler if present
                if (self.currentStep.nextOnClick) {
                    $("body").one("click.tour", self.currentStep.nextOnClick, $.proxy(self.handleTourClick, self));
                }

                // Create the new UI and return the element
                self.setContentForStep();

                var $el = $(self.currentStep.target);
                $el.popover({
                    html: true,
                    content: self.$tourContentWrapper,
                    title: self.currentStep.title,
                    placement: self.currentStep.placement,
                    trigger: "manual",
                    container: "body"
                });
                return $el;
            });

        // Show the current step if we were visible before
        if (isVisible) {
            this.show();
        }
    };

    // Show the UI
    TourUi.prototype.show = function () {
        var self = this;

        this.uiElementPromise.done(function($el) {
            if (self.visible === true) return;

            // Show the UI
            $el.popover("show");
            self.visible = true;
        });
    };

    // Hide the UI
    TourUi.prototype.hide = function () {
        var self = this;

        this.uiElementPromise.done(function($el) {
            if (self.visible === false) return;
            
            // Hide the UI
            $el.popover("hide");
            self.visible = false;
        });
    };

    // Handles clicks on the tour buttons in the UI and fires the appropriate event
    TourUi.prototype.handleTourClick = function(e) {
        var $target = $(e.currentTarget);
        if (this.currentStep.showNextButton !== false && $target.hasClass("tour-buttons-next")) {
            this.$element.trigger("nextClick");
        } else if (this.currentStep.showPreviousButton !== false && $target.hasClass("tour-buttons-previous")) {
            this.$element.trigger("previousClick");
        } else if (this.currentStep.nextOnClick && $target.is(this.currentStep.nextOnClick)) {
            this.$element.trigger("nextClick");
        }
    };

    // Set the content for the current step
    TourUi.prototype.setContentForStep = function () {
        // Set basic content
        this.$tourContent.html(this.currentStep.content);

        // Show or hide buttons
        if (this.currentStep.showPreviousButton === false) {
            this.$previousButton.hide();
        } else {
            this.$previousButton.show();
        }

        if (this.currentStep.showNextButton === false) {
            this.$nextButton.hide();
        } else {
            this.$nextButton.show();
        }
    };

    // Create the custom KO binding
    ko.bindingHandlers.tour = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var val = ko.unwrap(valueAccessor());

            // Create tour UI and store on the element
            var tourUi = new TourUi(element);

            var $el = $(element);
            $el.on("nextClick", val.next).on("previousClick", val.previous);
            $el.data("tour", tourUi);
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var val = ko.unwrap(valueAccessor());
            var currentStep = ko.unwrap(val.currentStep);
            var showCurrentStep = ko.unwrap(val.enabled);

            var tourUi = $(element).data("tour");

            // Set the step and show/hide as necessary
            tourUi.setStep(currentStep);
            if (showCurrentStep) {
                tourUi.show();
            } else {
                tourUi.hide();
            }
        }
    };
});