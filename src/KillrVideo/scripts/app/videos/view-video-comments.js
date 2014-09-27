define(["knockout", "jquery", "moment", "knockout-postbox"], function(ko, $, moment) {
    // ViewModel for individual comment on video
    function singleCommentViewModel(data) {
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

    // View model for video comments
    return function(params) {
        var self = this;

        // The number of comments per page to retrieve
        var pageSize = 10;

        // The list of currently loaded comments
        self.comments = ko.observableArray();

        // The comment id for the first comment on the next page if available
        self.firstCommentIdOnNextPage = ko.observable(null);

        // If more pages are available for viewing or not
        self.morePagesAvailable = ko.computed(function() {
            return self.firstCommentIdOnNextPage() !== null;
        });

        // Whether we're loading another page of comments
        self.loadingNextPage = ko.observable(false);

        // Loads a page of more comments
        self.loadNextPage = function () {
            // Indicate we're loading
            self.loadingNextPage(true);

            var ajaxData = {
                videoId: params.videoId,
                pageSize: pageSize + 1,       // Going to show 10 at a time, so get an extra record
                firstCommentIdOnPage: self.firstCommentIdOnNextPage()
            };

            $.ajax({
                type: "POST",
                url: "/Comments/ByVideo",
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).done(function(response) {
                if (!response.success)
                    return;

                // If we got the extra record, remove it and save the comment Id for loading subsequent pages
                if (response.data.comments.length === pageSize + 1) {
                    self.firstCommentIdOnNextPage(response.data.comments.pop().commentId);
                } else {
                    self.firstCommentIdOnNextPage(null);
                }
                
                // Add the comments to the array
                if (response.data.comments.length > 0) {
                    // Rather than push one-at-a-time and notifying for each push, only notify at the end of adding all comments
                    var commentsArray = self.comments();
                    self.comments.valueWillMutate();
                    for (var i = 0; i < response.data.comments.length; i++) {
                        var commentModel = new singleCommentViewModel(response.data.comments[i]);
                        commentsArray.push(commentModel);
                    }
                    self.comments.valueHasMutated();
                }
            }).always(function() {
                self.loadingNextPage(false);
            });
        };

        // Listen for new comments being added
        ko.postbox.subscribe("latestNewComment" + params.videoId, function (newValue) {
            // Add the new comment to the top of the list of comments
            if (newValue) {
                self.comments.splice(0, 0, new singleCommentViewModel(newValue));
            }
        });

        // Load the initial page of comments
        self.loadNextPage();
    };
});