define(["knockout", "text!./add-youtube.tmpl.html", "knockout-validation"], function (ko, htmlString) {
    // ViewModel for adding YouTube video
    function addYouTubeViewModel(params) {
        var self = this;

        // The URL of the YouTube video
        self.youTubeUrl = ko.observable("").extend({
            required: true,
            // Custom validator to make sure the YouTube URL gave us a valid location (i.e. has the v= parameter)
            validation: {
                validator: function(val) {
                    return self.youTubeVideoId();
                },
                message: "Provide a valid YouTube video URL"
            }
        });

        // Parse just the video Id from the YouTube URL
        self.youTubeVideoId = ko.computed(function() {
            var url = self.youTubeUrl();
            if (url)
                return getParameterByName("v", url);
            return "";
        });

        // The image URL for a preview
        self.youTubePreviewImageUrl = ko.computed(function() {
            var videoId = self.youTubeVideoId();
            if (videoId)
                return "//img.youtube.com/vi/" + videoId + "/hqdefault.jpg";
            return "";
        });

        // Saves the video and returns a promise for the URL where it can be viewed
        self.saveVideo = function (videoDetails) {
            // Add the YouTube video id and then save
            videoDetails.youTubeVideoId = self.youTubeVideoId();

            return $.post("/youtube/add", videoDetails)
                .then(function (response) {
                    // If there was some problem, return a rejected deferred so failure can run
                    if (!response.success)
                        return $.Deferred().reject().promise();

                    // Otherwise, the addvideo method should return the URL where the video can be viewed
                    return response.data.viewVideoUrl;
                });
        };

        // Gets a query string parameter by name (from http://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript)
        function getParameterByName(name, url) {
            var match = RegExp('[?&]' + name + '=([^&]*)').exec(url);
            return match && decodeURIComponent(match[1].replace(/\+/g, ' '));
        }

        // Pass self back to parent
        params.selectedSourceModel(self);
    };

    // Return KO component definition
    return { viewModel: addYouTubeViewModel, template: htmlString };
});