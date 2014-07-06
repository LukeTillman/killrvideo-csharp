define(["moment"], function (moment) {
    // Return viewModel for individual comment by user
    return function (data) {
        var self = this;

        // Most properties are just copies of the data from the server
        self.videoViewUrl = data.videoViewUrl;
        self.videoName = data.videoName;
        self.videoPreviewImageLocation = data.videoPreviewImageLocation;
        self.comment = data.comment;

        // Create a "X hours/days/etc. ago" property from the timestamp of the comment
        self.commentTimestampAgo = moment(data.commentTimestamp).fromNow();
    }
});