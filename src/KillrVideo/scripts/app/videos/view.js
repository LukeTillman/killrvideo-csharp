require([
        "knockout", "jquery", "app/videos/view-star-rating", "app/videos/view-video-comments", "app/videos/view-add-comment",
        "videojs", "lib/knockout-perfect-scrollbar", "lib/knockout-expander", "app/common", "app/shared/header"
    ],
    function(ko, $, ratingsModel, videoCommentsModel, addCommentModel) {
        // Bind the main content area when DOM is ready
        $(function () {
            // Get the video Id (TODO: better way to pass data?)
            var videoId = $("#video-id").val();
            var isLoggedIn = $.parseJSON($("#is-logged-in").val()); // Convert to boolean when getting string value

            ko.applyBindings({
                ratingsModel: new ratingsModel({ videoId: videoId }),
                videoCommentsModel: new videoCommentsModel({ videoId: videoId }),
                addCommentModel: new addCommentModel({ videoId: videoId, isLoggedIn: isLoggedIn })
            }, $("#body-wrapper").get(0));
        });
    });