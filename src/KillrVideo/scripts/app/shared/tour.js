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

        // The step object for the current step
        self.currentStep = ko.pureComputed(function() {
            var idx = self.currentStepIndex();
            var step = steps[idx];

            // See if we're on the correct page for the step, possibly by running a RegEx
            var onPage = (typeof step.page === "string")
                ? step.page === window.location.pathname.toLowerCase()
                : step.page.test(window.location.pathname.toLowerCase());

            return onPage ? step : null;
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
                // Set current step state
                var newIdx = curIdx - 1;
                self.currentStepIndex(newIdx);

                // Do back button navigation if necessary
                if (steps[curIdx].page !== steps[newIdx].page) {
                    window.history.back();
                }
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

        // Overall whether the tour is enabled
        self.enabled = ko.pureComputed(function () {
            return self.userEnabled();
        });
    };
});