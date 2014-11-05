define(["knockout", "jquery", "moment", "text!./user-comments-list.tmpl.html"], function(ko, $, moment, htmlString) {
    // A view model for an individual user comment
    function singleCommentViewModel(data) {
        var self = this;

        // Most properties are just copies of the data from the server
        self.videoViewUrl = data.videoViewUrl;
        self.videoName = data.videoName;
        self.videoPreviewImageLocation = data.videoPreviewImageLocation;
        self.comment = data.comment;

        // Create a "X hours/days/etc. ago" property from the timestamp of the comment
        self.commentTimestampAgo = moment(data.commentTimestamp).fromNow();
    }

    // A view model for user comments
    function userCommentsViewModel(params) {
        var self = this;

        // Number of records per page to show
        var pageSize = 10;

        // All the comments we've loaded
        self.comments = ko.observableArray([]);

        // The id of the first comment on the next page from the server
        self.firstCommentIdOnNextPage = ko.observable(null);

        // Whether or not the user has more comments available
        self.morePagesAvailable = ko.computed(function() {
            return self.firstCommentIdOnNextPage() !== null;
        });

        // Whether of not we are loading the next page
        self.loadingNextPage = ko.observable(false);

        // Loads the next page of comments for the user
        self.loadNextPage = function() {
            // Indicate we're loading the next page
            self.loadingNextPage(true);

            var ajaxData = {
                userId: params.userId,
                pageSize: pageSize + 1, // Always get one more record than we actually need to tell whether there is a next page
                firstCommentIdOnPage: self.firstCommentIdOnNextPage()
            };

            $.ajax({
                type: "POST",
                url: "/Comments/ByUser",
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).done(function(response) {
                if (!response.success)
                    return [];

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

        // Load the first page of comments
        self.loadNextPage();
    }

    // Return a KO component definition
    return { viewModel: userCommentsViewModel, template: htmlString };
});