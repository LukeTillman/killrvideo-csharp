define(["knockout", "jquery", "text!./video-star-rating.tmpl.html"], function(ko, $, htmlString) {
    function starRatingViewModel(params) {
        var self = this;

        self.videoId = params.videoId;

        // The available ratings (1-5 stars)
        self.availableRatings = [1, 2, 3, 4, 5];

        // Whether or not rating is enabled
        self.ratingEnabled = ko.observable(false);

        // The current rating data
        self.currentUserRating = ko.observable(0);
        self.ratingsCount = ko.observable(0);
        self.ratingsSum = ko.observable(0);
        self.averageRating = ko.computed(function() {
            var count = self.ratingsCount();
            if (count <= 0)
                return 0;

            var avg = self.ratingsSum() / count;
            return avg.toFixed(1);
        });

        // The current rating to display in whole stars
        self.displayRating = ko.computed(function () {
            var avg = self.averageRating();
            return Math.floor(avg);
        });

        // The "proposed" rating (reacts to user hovering if enabled)
        self.proposedRating = ko.observable(0);

        // React to mouseover and track the proposed rating
        self.trackProposedRating = function (rating) {
            // Only track proposed ratings if rating is enabled
            if (self.ratingEnabled() === false)
                return;

            self.proposedRating(rating);
        };

        // React to mouseout and reset the proposed rating to 0
        self.resetProposedRating = function () {
            if (self.ratingEnabled() === false)
                return;

            self.proposedRating(0);
        };

        // Rates a video
        self.rateVideo = function (ratingClicked) {
            // Just bail if not enabled
            if (self.ratingEnabled() === false) {
                return;
            }

            // Disable rating so we only accept one click
            self.ratingEnabled(false);

            // Record the user's click on the view model
            self.currentUserRating(ratingClicked);
            var count = self.ratingsCount();
            self.ratingsCount(count + 1);
            var sum = self.ratingsSum();
            self.ratingsSum(sum + ratingClicked);
            
            // Post the rating to the server
            $.post("/videos/rate", { videoId: params.videoId, rating: ratingClicked }).done(function(response) {
                // TODO: Handle failures?
            });
        };

        // Load rating data from the server
        $.getJSON("/videos/getratings", { videoId: params.videoId }).done(function (response) {
            // If for some reason we failed, just bail
            if (response.data.success === false)
                return;

            // Set the rating data on the view model
            self.currentUserRating(response.data.currentUserRating);
            self.ratingsCount(response.data.ratingsCount);
            self.ratingsSum(response.data.ratingsSum);

            // If the current user is logged in but has not yet rated the video, enable rating
            if (response.data.currentUserRating === 0 && response.data.currentUserLoggedIn === true)
                self.ratingEnabled(true);
        });
    };

    // Return KO component defintion
    return { viewModel: starRatingViewModel, template: htmlString };
});