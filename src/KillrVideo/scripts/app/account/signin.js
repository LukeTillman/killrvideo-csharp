define(["knockout", "jquery", "knockout-validation"], function (ko, $) {
    // Return view model for sign in page
    return function signInViewModel() {
        var self = this;

        self.emailAddress = ko.observable("").extend({ required: true, email: true });
        self.password = ko.observable("").extend({ required: true });

        // Whether or not a sign-in is in progress
        self.inProgress = ko.observable(false);

        // Any validation errors
        self.validationErrors = ko.validation.group(self);

        self.signInUrl = "/account/signinuser";

        // Signs in via an AJAX call to the server and redirects to the URL specified on success
        self.signIn = function() {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Indicate we're signing in
            self.inProgress(true);

            $.post(self.signInUrl, { emailAddress: self.emailAddress(), password: self.password() })
                .done(function(response) {
                    // If successful, redirect to the page specified
                    if (response.success) {
                        // Default to home page if no redirect Url is specified
                        var redirectUrl = response.data.afterLoginUrl ? response.data.afterLoginUrl : "/";
                        window.location.href = redirectUrl;
                    } else {
                        self.inProgress(false);
                    }
                })
                .fail(function() {
                    // Always toggle in progress back to false on failure
                    self.inProgress(false);
                });
        };
    };
});