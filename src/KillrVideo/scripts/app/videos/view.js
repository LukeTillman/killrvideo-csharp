define([
        "jquery", "app/videos/view-star-rating", "app/videos/view-video-comments", "app/videos/view-add-comment",
        "lib/knockout-perfect-scrollbar", "lib/knockout-expander"
    ],
    function ($, ratingsModel, videoCommentsModel, addCommentModel) {
        // Return view model for viewing a video
        return function viewVideoViewModel() {
            var self = this;

            // Get the video Id (TODO: better way to pass data?)
            var videoId = $("#video-id").val();
            var isLoggedIn = $.parseJSON($("#is-logged-in").val()); // Convert to boolean when getting string value

            // Video ratings
            self.ratingsModel = new ratingsModel({ videoId: videoId });

            // Comments
            self.videoCommentsModel = new videoCommentsModel({ videoId: videoId });

            // Adding a new comment
            self.addCommentModel = new addCommentModel({ videoId: videoId, isLoggedIn: isLoggedIn });
        };
    });