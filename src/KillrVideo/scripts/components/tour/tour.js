define(["knockout", "jquery", "text!./tour.tmpl.html", "./main-tour", "lib/knockout-extenders", "lib/knockout-popover", "bootstrap", "knockout-postbox"], function (ko, $, htmlString, mainTour) {
    function tourViewModel(params) {
        var self = this;

        // An array of tour step definitions (normalized with some default properties if not set)
        var steps = $.map(mainTour.steps, function(step) {
            var stepWithDefaults = $.extend({
                showNextButton: true,
                showPreviousButton: true,
                showEndButton: false,
                callToAction: null,
                contentClass: null,
                links: null
            }, step);

            if (step.onShow) {
                stepWithDefaults.onShow = $.proxy(step.onShow, stepWithDefaults, self);
            }

            if (step.onHide) {
                stepWithDefaults.onHide = $.proxy(step.onHide, stepWithDefaults, self);
            }

            return stepWithDefaults;
        });

        // A unique identifier for the tour
        self.tourId = mainTour.tourId;

        // An object with properties representing the different pages on the tour
        self.pages = mainTour.pages;

        // The currently logged in user (the header model publishes this value)
        self.loggedInUser = ko.observable().subscribeTo("loggedInUser", true);

        // The index of the current step
        self.currentStepIndex = ko.observable(0).extend({ persist: self.tourId + ".index" });

        // Whether or not we're on the correct page for the current step
        self.onCorrectPage = ko.pureComputed(function () {
            var idx = self.currentStepIndex();
            var step = steps[idx];

            // Get the page by name (key)
            var pageUrl = self.pages[step.page].url;

            // See if we're on the correct page for the step, possibly by running a RegEx
            return (typeof pageUrl === "string")
                ? pageUrl === window.location.pathname.toLowerCase()
                : pageUrl.test(window.location.pathname.toLowerCase());
        });

        // The step object for the current step
        self.currentStep = ko.pureComputed(function () {
            var rightPage = self.onCorrectPage();
            if (!rightPage) return null;

            // See if the step has a promsie to be fulfilled first
            var step = steps[self.currentStepIndex()];
            if (!step.beforeShowPromise) return step;

            return step.beforeShowPromise().then(function() { return step; });
        }).extend({ async: null });

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

                // Navigate to previous page if necessary
                if (steps[newIdx].page !== steps[curIdx].page) {
                    self.navigateToCurrentPage(false);
                }
            }
        };

        // Whether or not the tour is enabled
        self.enabled = ko.observable(true).extend({ persist: self.tourId + ".enabled" });

        // Disable the tour
        self.disable = function () {
            self.enabled(false);
        };

        // Restart the tour from the beginning
        self.restart = function () {
            self.currentStepIndex(0);
            self.navigateToCurrentPage(true);
        };

        // URLs the user has visited for various pages
        self.pageUrls = ko.observable({}).extend({ persist: self.tourId + ".pageUrls" });

        // Resume the tour from where you left off
        self.resume = function () {
            self.navigateToCurrentPage(true);
        };

        // Navigate to the page for the current step (taking into account authentication) and optionally enable the tour
        self.navigateToCurrentPage = function(enable) {
            // Get the URL we'll be going to
            var pageKey = steps[self.currentStepIndex()].page;
            var pageUrl = self.pageUrls()[pageKey];

            // Make sure authentication state is correct before navigating
            var page = self.pages[pageKey];
            var isLoggedIn = self.loggedInUser().isLoggedIn;
            if (page.authenticated && isLoggedIn === false) {
                // Log them in before going to the page
                $.post("/account/signinuser", { emailAddress: "guidedtour@killrvideo.com", password: "guidedtour" }).done(function () {
                    if (enable) self.enabled(true);
                    window.location.href = pageUrl;
                });
            } else if (page.authenticated === false && isLoggedIn) {
                // Log them out before going to the page
                $.get("/account/signout").done(function () {
                    if (enable) self.enabled(true);
                    window.location.href = pageUrl;
                });
            } else {
                if (enable) self.enabled(true);

                // Just go to the page if not already there
                if (window.location.href !== pageUrl) {
                    window.location.href = pageUrl;
                }
            }
        };

        // If we're not on the correct page when the model is initially loaded, disable the tour
        if (self.onCorrectPage() === false) {
            self.enabled(false);
        } else if (self.enabled() === true) {
            // If we're on the correct page and enabled, save the URL so the page can be navigated to if resumed or previous button is used
            var stepIdx = self.currentStepIndex();
            var pageKey = steps[stepIdx].page;
            var urls = self.pageUrls();
            urls[pageKey] = window.location.href;
            self.pageUrls(urls);
        }
    }

    // Return KO component definition
    return { viewModel: tourViewModel, template: htmlString };
});