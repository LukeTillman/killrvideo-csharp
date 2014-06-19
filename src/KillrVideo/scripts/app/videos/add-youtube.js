define(["knockout", "knockout-validation"], function (ko) {
    // Review ViewModel for adding YouTube video
    return function () {
        var self = this;

        // The URL of the YouTube video
        self.youTubeUrl = ko.observable("").extend({
            required: true,
            // Custom validator to make sure the YouTube URL gave us a valid location (i.e. has the v= parameter)
            validation: {
                validator: function(val) {
                    return self.location();
                },
                message: "Provide a valid YouTube video URL"
            }
        });

        // Parse just the video Id from the YouTube URL
        self.location = ko.computed(function() {
            var url = self.youTubeUrl();
            if (url)
                return getParameterByName("v", url);
            return "";
        });

        // Location type is always just youtube for this source
        self.locationType = ko.observable("youtube");

        // The image URL for a preview
        self.youTubePreviewImageUrl = ko.computed(function() {
            var videoId = self.location();
            if (videoId)
                return "//img.youtube.com/vi/" + videoId + "/hqdefault.jpg";
            return "";
        });

        // Gets a query string parameter by name (from http://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript)
        function getParameterByName(name, url) {
            var match = RegExp('[?&]' + name + '=([^&]*)').exec(url);
            return match && decodeURIComponent(match[1].replace(/\+/g, ' '));
        }
    };
});