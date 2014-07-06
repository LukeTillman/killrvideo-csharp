define(["moment"], function(moment) {
    // Return viewModel for individual comment on video
    return function (data) {
        var self = this;

        // Most properties are just copies of the data from the server
        self.userProfileUrl = data.userProfileUrl;
        self.userFirstName = data.userFirstName;
        self.userLastName = data.userLastName;
        self.userGravatarImageUrl = data.userGravatarImageUrl;
        self.comment = data.comment;

        // Create a "X hours/days/etc. ago" property from the timestamp of the comment
        self.commentTimestampAgo = moment(data.commentTimestamp).fromNow();
    }
});