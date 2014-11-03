define(["knockout", "text!./add-youtube.tmpl.html", "knockout-validation", "knockout-postbox"], function (ko, htmlString) {
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

        // The video Id currently selected
        self.selectedYouTubeVideoId = ko.observable("");

        // The image URL for a preview
        self.youTubePreviewImageUrl = ko.computed(function() {
            var videoId = self.selectedYouTubeVideoId();
            if (videoId)
                return "//img.youtube.com/vi/" + videoId + "/hqdefault.jpg";
            return "";
        });

        // Whether it's OK to show the rest of the common details form entry
        self.showCommonDetails = ko.observable(false).syncWith("add-video-showCommonDetails", false, false);

        // Any validation errors
        self.validationErrors = ko.validation.group([self.youTubeUrl]);

        // Looks for enter key presses and if found, attempts to set the YouTube video selection
        self.setSelectionOnEnter = function(data, event) {
            if (event.keyCode === 13) {
                // Need blur or else validator will not have received the newest value yet
                $(event.target).blur();

                // Set the selection
                self.setSelection();
                return false;
            }
            return true;
        };

        // Gets the information for the selected YouTube video and sets the selection
        self.setSelection = function() {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Set the selection and indicate it's OK to show the common details entry
            self.selectedYouTubeVideoId(self.youTubeVideoId());
            self.showCommonDetails(true);
            self.savingAvailable(true);
        };

        // Clears the selected YouTube video
        self.clearSelection = function() {
            self.youTubeUrl("");
            self.selectedYouTubeVideoId("");
            self.validationErrors.showAllMessages(false);
            self.showCommonDetails(false);
        };

        // Whether or not we're saving
        self.saving = ko.observable(false).syncWith("add-video-saving");

        // Whether or not saving is available
        self.savingAvailable = ko.observable(false).syncWith("add-video-savingAvailable");

        // The URL where the newly added video can be viewed
        self.viewVideoUrl = ko.observable("").publishOn("add-video-viewVideoUrl");

        // Subscribe to the save video click on the parent and save the video
        ko.postbox.subscribe("add-youtube-save-clicked", function (videoDetails) {
            // Make sure we've indicated we're saving
            self.saving(true);

            // Make sure we're valid
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                self.saving(false);
                return;
            }

            // Add the YouTube video id and then save
            videoDetails.youTubeVideoId = self.youTubeVideoId();

            $.ajax({
                    type: "POST",
                    url: "/youtube/add",
                    data: JSON.stringify(videoDetails),
                    contentType: "application/json",
                    dataType: "json"
                }).then(function(response) {
                    // If there was some problem, just bail
                    if (!response.success)
                        return;

                    // Indicate the URL where the video can be viewed
                    self.viewVideoUrl(response.data.viewVideoUrl);
                })
                .always(function() {
                    self.saving(false);
                });
        });
        
        // Gets a query string parameter by name (from http://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript)
        function getParameterByName(name, url) {
            var match = RegExp('[?&]' + name + '=([^&]*)').exec(url);
            return match && decodeURIComponent(match[1].replace(/\+/g, ' '));
        }
    };

    // Return KO component definition
    return { viewModel: addYouTubeViewModel, template: htmlString };
});