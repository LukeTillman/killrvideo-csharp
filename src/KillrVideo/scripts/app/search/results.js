require(["knockout", "jquery", "app/shared/video-preview-pager", "app/common", "app/shared/header"], function (ko, $, videoPreviewPagerModel) {
    // Model for a single search result
    function searchResultVideo(data) {
        var self = this;

        // The name of the video
        self.name = data.name;

        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // The preview image for the video
        self.videoPreviewImageUrl = data.previewImageLocation;
    }

    // Bind the main content area when DOM is ready
    $(function () {
        // Include the tag that was searched for in the ajaxData
        var tag = $("#tag-searched-for").val();

        // Just use a simple object as the model for the page and apply bindings
        var pageModel = {
            searchResultsList: new videoPreviewPagerModel({
                url: '/search/videos',
                ajaxData: {
                    tag: tag
                },
                pageSize: 8,
                groupSize: 4,
                videoModelConstructor: searchResultVideo
            })
        };

        ko.applyBindings(pageModel, $("#body-wrapper").get(0));
    });
});