define(["app/shared/video-preview-pager", "text!./user-videos-table.tmpl.html"], function (videoPagerModel, htmlString) {
    // A view model for an individual video preview in the list of videos
    function videoPreviewRow(data) {
        var self = this;

        // The name of the video
        self.name = data.name;

        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // A pretty-formatted version of the added date
        self.addedDatePretty = moment(data.addedDate).format("LLL");
    }
    
    // Return KO component definition that uses the shared model and the HTML template required above
    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                // The params should include a user id
                var userId = params.userId;

                // Create an instance of the shared view model with some parameters set
                return new videoPagerModel({
                    url: '/videos/byuser',
                    ajaxData: {
                        userId: userId
                    },
                    pageSize: 10,
                    videoModelConstructor: videoPreviewRow
                });
            }
        },
        template: htmlString
    };
});