require(["knockout", "jquery", "knockout-validation", "knockout-postbox", "lib/knockout-bootstrap-select", "app/common", "app/shared/header"], function (ko, $) {
    // ViewModel for the add video page
    function addVideoViewModel() {
        var self = this;

        // Common video details
        self.name = ko.observable("").extend({ required: true }).subscribeTo("add-video-name");
        self.description = ko.observable("").subscribeTo("add-video-description");
        self.tags = ko.observable("").subscribeTo("add-video-tags");      // TODO:  Array with select2 binding?

        // The currently selected video source
        self.selectedSource = ko.observable().extend({ required: true });

        // Whether to show the common details entry fields
        self.showCommonDetails = ko.observable(false).syncWith("add-video-showCommonDetails");

        // Whether or not saving is available
        self.savingAvailable = ko.observable(false).syncWith("add-video-savingAvailable");

        // Whether or not we're saving
        self.saving = ko.observable(false).syncWith("add-video-saving");

        // The URL to go and view the video once saving has been successful
        self.viewVideoUrl = ko.observable("").subscribeTo("add-video-viewVideoUrl");

        // Adds the video via an AJAX call to the server
        self.addVideo = function () {
            // Indicate we're saving
            self.saving(true);

            // Check for any validation problems
            var validationErrors = ko.validation.group([self.name, self.description, self.tags, self.selectedSource]);
            if (validationErrors().length > 0) {
                validationErrors.showAllMessages();
                self.saving(false);
                return;
            }

            // Pull video details into a JS object
            var videoDetails = {
                name: self.name(),
                description: self.description(),
                tags: self.tags()
            };

            // Publish a message so the child component (YouTube, Upload, etc.) can do the actual saving
            var queueName = self.selectedSource() + "-save-clicked";
            ko.postbox.publish(queueName, videoDetails);
        };
    };

    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings(new addVideoViewModel(), $("#body-wrapper").get(0));
    });
});

