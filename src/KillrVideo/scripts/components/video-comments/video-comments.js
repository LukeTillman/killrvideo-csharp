define(["knockout", "jquery", "moment", "text!./video-comments.tmpl.html", "knockout-validation"], function(ko, $, moment, htmlString) {
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
    function videoCommentsViewModel(params) {
        var self = this;

        // The number of comments per page to retrieve
        var pageSize = 10;

        // Whether a user is logged in
        self.isLoggedIn = params.isLoggedIn;

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

        // The text of the new comment
        self.newComment = ko.observable("").extend({ required: true });

        // Any validation errors
        self.validationErrors = ko.validation.group(self);

        // Whether or not we're in the process of adding a new comment
        self.addingComment = ko.observable(false);

        // Adds a new comment from the currently logged in user
        self.addComment = function () {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Add the comment
            self.addingComment(true);

            var ajaxData = {
                videoId: params.videoId,
                comment: self.newComment()
            };

            $.ajax({
                type: "POST",
                url: "/Comments/Add",
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).done(function (response) {
                if (!response.success)
                    return;
                
                // We should get a comment returned to us on success, so add the new comment to the top of the list
                // of comments so it shows up
                self.comments.splice(0, 0, new singleCommentViewModel(response.data));
                self.newCommentAdded(true);

            }).always(function () {
                self.addingComment(false);
            });
        };

        // Whether or not a new comment was successfully added
        self.newCommentAdded = ko.observable(false);

        // Resets the view model for a new comment
        self.resetForNewComment = function() {
            self.newComment("");
            self.validationErrors.showAllMessages(false);
            self.newCommentAdded(false);
        };

        // Load the initial page of comments
        self.loadNextPage();
    };

    // Return KO component definition
    return { viewModel: videoCommentsViewModel, template: htmlString };
});