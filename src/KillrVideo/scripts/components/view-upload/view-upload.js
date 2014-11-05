define(["jquery", "knockout", "text!./view-upload.tmpl.html", "videojs"], function ($, ko, htmlString, videojs) {
    // Add a custom binding for videos using the videojs plugin
    ko.bindingHandlers.videojs = {
        after: ["attr"],
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            // Extend inner binding context with values from outer object value and then bind descendants
            var innerBindingContext = bindingContext.extend(valueAccessor);
            ko.applyBindingsToDescendants(innerBindingContext, element);

            // Add videoJs classes to the element
            var $el = $(element);
            $el.addClass("video-js vjs-default-skin");

            // Get any options that might need to be passed
            var val = ko.unwrap(valueAccessor());
            var opt = val.options || {};

            // Look for any events
            var eventsBinding = allBindings.get("videojsEvents") || {};

            // Attach the videoJs player
            videojs($el.get(0), opt, function() {
                var player = this;

                // Attach event handlers specified in the videojsEvents binding
                $.each(eventsBinding, function(eventName, handler) {
                    player.on(eventName, handler);
                });
            });

            // Tell KO not to bind descendants since we already did it
            return { controlsDescendantBindings: true };
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        }
    };

    // View model for the component
    function viewUploadViewModel(params) {
        var self = this;

        // The location for the video
        self.location = params.location;

        // Whether or not playback was started for the video
        self.playbackStarted = false;

        // Records playback stat for the video
        self.recordPlayback = function() {
            if (self.playbackStarted === true)
                return;

            self.playbackStarted = true;
            $.ajax({
                type: "POST",
                url: "/playbackstats/started",
                data: JSON.stringify({ videoId: params.videoId }),
                contentType: "application/json",
                dataType: "json"
            });
        };
    }

    // Return KO component definition
    return { viewModel: viewUploadViewModel, template: htmlString };
});