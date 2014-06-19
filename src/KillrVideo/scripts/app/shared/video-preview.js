// View model for a single video preview
define(["moment"], function (moment) {
    // Return view model
    return function(data) {
        var self = this;

        // The name of the video
        self.name = data.name;
        
        // URL to the video will be /video/guidIdString
        self.videoUrl = "/view/" + data.videoId;

        // The date the video was added on
        self.addedDate = data.addedDate;

        // A pretty-formatted version of the added date
        self.addedDatePretty = moment(data.addedDate).format("LLL");

        // The preview image for the video
        self.videoPreviewImageUrl = data.previewImageLocation;
    };
});