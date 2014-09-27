define(["knockout", "knockout-postbox"], function(ko) {
    return function(params) {
        var self = this;

        // Whether a user is logged in
        self.isLoggedIn = params.isLoggedIn;

        // The text of the new comment
        self.newComment = ko.observable();

        // Track focus on the comment form elements
        self.editingComment = ko.observable(false);

        // Whether or not we're in the process of adding a new comment
        self.addingComment = ko.observable(false);

        // Adds a new comment from the currently logged in user
        self.addComment = function () {
            self.addingComment(true);

            // Make sure we have a comment
            var newComment = self.newComment();
            if (!newComment) {
                self.editingComment(true);
                self.addingComment(false);
                return;
            }
            
            // Add the comment
            var ajaxData = {
                videoId: params.videoId,
                comment: newComment
            };

            $.ajax({
                type: "POST",
                url: "/Comments/Add",
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).done(function(response) {
                if (!response.success)
                    return;

                // We should get a comment returned to us on success, so save the new comment and indicate a new comment was added
                self.latestNewComment(response.data);
                self.newCommentAdded(true);

                // Done adding comment
                self.addingComment(false);
            }).fail(function() {
                // If failure for some reason, go back to editing the comment
                self.editingComment(true);
                self.addingComment(false);
            });
        };

        // The latest new comment added
        self.latestNewComment = ko.observable(null).publishOn("latestNewComment" + params.videoId);

        // Whether or not a new comment was successfully added
        self.newCommentAdded = ko.observable(false);

        // Resets the view model for a new comment
        self.resetForNewComment = function() {
            self.newComment("");
            self.newCommentAdded(false);
            self.editingComment(true);
        };
    };
});