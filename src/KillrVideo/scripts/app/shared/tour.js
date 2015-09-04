define(["knockout", "lib/knockout-extenders"], function (ko) {
    // Return a view model for the tour
    return function tourViewModel(params) {
        var self = this;

        // An array of tour step definitions
        var steps = params.steps;

        // A unique identifier for the tour
        self.tourId = params.tourId;

        // The index of the current step
        self.currentStepIndex = ko.observable(0).extend({ persist: self.tourId + ".index" });

        self.currentStep = ko.pureComputed(function() {
            var idx = self.currentStepIndex();
            return steps[idx];
        });

        // Go to next step
        self.next = function () {
            var curIdx = self.currentStepIndex();
            if (curIdx < steps.length - 1) {
                self.currentStepIndex(curIdx + 1);
            }
        };

        // Go to previous step
        self.previous = function () {
            var curIdx = self.currentStepIndex();
            if (curIdx > 0) {
                self.currentStepIndex(curIdx - 1);
            }
        };

        // Whether or not the tour is enabled by the user
        self.userEnabled = ko.observable(true).extend({ persist: self.tourId + ".userEnabled" });

        // Sets user enabled to true
        self.enable = function () {
            self.userEnabled(true);
        };

        // Sets user enabled to false
        self.disable = function () {
            self.userEnabled(false);
        };

        // Whether or not we're on the correct page for the current tour step
        self.onCorrectPage = ko.pureComputed(function() {
            // Test the page property of the current step, possibly by running a RegEx
            var currentStep = self.currentStep();
            return (typeof currentStep.page === "string")
                ? currentStep.page === window.location.pathname.toLowerCase()
                : currentStep.page.test(window.location.pathname.toLowerCase());
        });

        // Overall whether the tour is enabled
        self.enabled = ko.pureComputed(function () {
            return self.userEnabled() && self.onCorrectPage();
        });
    };
});