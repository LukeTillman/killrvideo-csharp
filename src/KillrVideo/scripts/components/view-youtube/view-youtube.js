define(["jquery", "text!./view-youtube.tmpl.html"], function ($, htmlString) {
    // Construct a promise that will be resolved when the YouTube player API is ready
    var youTubePlayerApiLoaded = $.Deferred();
    if (typeof (YT) == "undefined" || typeof (YT.Player) == "undefined") {
        window.onYouTubeIframeAPIReady = function() {
            youTubePlayerApiLoaded.resolve();
        };

        $.getScript("//www.youtube.com/iframe_api");
    } else {
        youTubePlayerApiLoaded.resolve();
    }
    
    function viewYouTubeViewModel(params, iframeEl) {
        var self = this;

        // The Url for the video
        self.youTubeUrl = "http://www.youtube.com/embed/" + params.location + "?enablejsapi=1";

        // The player instance, created when the API is loaded
        self.youTubePlayer = null;

        // Whether or not playback was started for the video
        self.playbackStarted = false;

        // When the API is loaded, create the player instance
        youTubePlayerApiLoaded.done(function() {
            self.youTubePlayer = new YT.Player(iframeEl, {
                events: {
                    // Record views when video is played back
                    "onStateChange": function(event) {
                        if (self.playbackStarted === false && event.data === YT.PlayerState.PLAYING) {
                            self.playbackStarted = true;
                            $.ajax({
                                type: "POST",
                                url: "/playbackstats/started",
                                data: JSON.stringify({ videoId: params.videoId }),
                                contentType: "application/json",
                                dataType: "json"
                            });
                        }
                    }
                }
            });
        });
    }
    
    // Return KO component definition
    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                var iframeEl = $(componentInfo.element).children("iframe").get(0);
                return new viewYouTubeViewModel(params, iframeEl);
            }
        },
        template: htmlString
    };
});

