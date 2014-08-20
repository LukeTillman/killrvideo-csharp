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

        // Adds the video via an AJAX call to the server
        self.addVideo = function () {
            // Check for any validation problems
            var validationErrors = ko.validation.group([self.name, self.description, self.tags, self.selectedSource], { deep: true });
            if (validationErrors().length > 0) {
                validationErrors.showAllMessages();
                return;
            }

            // Indicate we're saving
            self.saving(true);

            // Pull video details into a JS object
            var videoDetails = {
                name: self.name(),
                description: self.description(),
                tags: self.tags()
            };

            // Delegate to each video type to decide how to save itself
            var saved = self.selectedSource().model.saveVideo(videoDetails);

            // When saving is finished, act appropriately
            $.when(saved)
                .done(function(viewVideoUrl) {
                    // If saving is successful, indicate where the video can be viewed
                    self.viewVideoUrl(viewVideoUrl);
                })
                .always(function() {
                    // Always toggle saving back to false
                    self.saving(false);
                });
        };
    };
});

