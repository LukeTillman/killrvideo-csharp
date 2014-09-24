define(["jquery", "app/shared/video-preview-pager", "text!./video-preview-list.tmpl.html"], function ($, videoPagerModel, htmlString) {
    // A view model for an individual video preview
    function videoPreviewViewModel(data) {
        var self = this;

        // The name of the video
        self.name = data.name;

        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // The preview image for the video
        self.videoPreviewImageUrl = data.previewImageLocation;

        // Navigate to the video
        self.goToVideo = function() {
            window.location.href = self.videoUrl;
        }
    }

    // Return a KO component definition
    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                // Merge params data into object that specifies some settings needed by this component
                var setupData = $.extend(params, { videoModelConstructor: videoPreviewViewModel, pageSize: 4 });

                // Create an instance of the shared view model with some parameters set
                return new videoPagerModel(setupData);
            }
        },
        template: htmlString
    };
});