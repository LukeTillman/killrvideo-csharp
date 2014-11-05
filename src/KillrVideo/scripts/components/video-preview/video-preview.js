define(["jquery", "knockout", "moment", "text!./video-preview.tmpl.html"], function ($, ko, moment, htmlString) {
    // A view model for an individual video preview
    function videoPreviewViewModel(params) {
        var self = this;
        var data = params.data;

        // The name of the video
        self.name = data.name;

        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // The preview image for the video
        self.videoPreviewImageUrl = data.previewImageLocation;

        // How long ago the video was added
        self.addedAgo = moment(data.addedDate).fromNow();

        // The name of the video author
        self.authorName = data.authorFirstName + " " + data.authorLastName;

        // The number of times the video has been viewed
        self.views = data.views;

        // Handles clicks on the video preview
        self.doClick = function () {
            // If user specified a click function, do it
            if (params.onPreviewClick) {
                params.onPreviewClick(self);
                return;
            }

            // Otherwise, navigate to the video by default on click
            window.location.href = self.videoUrl;
        };
    }

    // Return a KO component definition
    return { viewModel: videoPreviewViewModel, template: htmlString };
});