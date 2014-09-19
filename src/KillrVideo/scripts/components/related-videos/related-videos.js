define(["app/shared/video-preview-pager", "text!./related-videos.tmpl.html"], function (videoPagerModel, htmlString) {
    // A view model for an individual video preview
    function videoPreviewViewModel(data) {
        var self = this;

        // The name of the video
        self.name = data.name;

        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // The preview image for the video
        self.videoPreviewImageUrl = data.previewImageLocation;
    }

    // Return KO component definition
    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                // Video Id should be set in params
                var videoId = params.videoId;

                // Create an instance of the shared view model with some parameters set
                return new videoPagerModel({
                    url: '/videos/related',
                    ajaxData: {
                        videoId: videoId
                    },
                    videoModelConstructor: videoPreviewViewModel
                });
            }
        },
        template: htmlString
    }
});