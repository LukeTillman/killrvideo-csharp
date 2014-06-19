define(["knockout", "jquery", "app/videos/add-sources", "knockout-validation"], function (ko, $, sourcesModel) {
    // Return ViewModel for adding a video
    return function() {
        var self = this;

        self.name = ko.observable("").extend({ required: true });
        self.description = ko.observable("");
        self.tags = ko.observable("");      // TODO:  Array with select2 binding?

        // The available video sources and the currently selected source
        self.availableSources = sourcesModel;
        self.selectedSource = ko.observable().extend({ required: true });

        // Whether or not we're saving
        self.saving = ko.observable(false);

        // The URL to go and view the video once saving has been successful
        self.viewVideoUrl = ko.observable("");

        // Any validation errors
        self.validationErrors = ko.validation.group(self, { deep: true });

        self.addUrl = "/videos/addvideo";

        // Adds the video via an AJAX call to the server
        self.addVideo = function () {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Indicate we're saving
            self.saving(true);

            // Pull all data into a JS object that can be posted
            var sourceModel = self.selectedSource().model;
            var postData = {
                name: self.name(),
                description: self.description(),
                tags: self.tags(),
                location: sourceModel.location(),
                locationType: sourceModel.locationType()
            };

            $.post(self.addUrl, postData)
                .done(function (response) {
                    // If successful, set the URL for viewing
                    if (response.success) {
                        self.viewVideoUrl(response.data.viewVideoUrl);
                    }
                })
                .always(function () {
                    // Always toggle saving back to false
                    self.saving(false);
                });
        };
    };
});

