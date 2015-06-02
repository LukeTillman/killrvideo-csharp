require(["knockout", "jquery", "knockout-validation", "app/common", "app/shared/header"], function (ko, $) {
    // ViewModel for registration page
    function registerViewModel() {
        var self = this;

        // Information about the new user
        self.firstName = ko.observable("").extend({ required: true });
        self.lastName = ko.observable("").extend({ required: true });
        self.emailAddress = ko.observable("").extend({ required: true, email: true });
        self.password = ko.observable("").extend({ required: true });
        self.confirmPassword = ko.observable("").extend({
            required: true,
            // Custom validation to make sure password fields match
            validation: {
                validator: function(val) {
                    return val === self.password();
                },
                message: "Passwords must match"
            }
        });

        // Whether or not we're saving
        self.saving = ko.observable(false);

        // The user Id once registration is successful
        self.registeredUserId = ko.observable("");

        // Any validation errors
        self.validationErrors = ko.validation.group(self);

        self.registerUrl = "/account/registeruser";

        // Saves the new user's registration via AJAX call
        self.registerUser = function () {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Indicate we're saving
            self.saving(true);

            // Collect all the new user's data for posting
            var postData = {
                firstName: self.firstName(),
                lastName: self.lastName(),
                emailAddress: self.emailAddress(),
                password: self.password()
            };

            // Post to the server
            $.post(self.registerUrl, postData)
                .done(function(response) {
                    // If successful, indicate the user's id
                    if (response.success) {
                        self.registeredUserId(response.data.userId);
                    }
                })
                .always(function() {
                    // Always toggle saving back to false
                    self.saving(false);
                });
        };
    }

    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings(new registerViewModel(), $("#body-content").get(0));
    });
});