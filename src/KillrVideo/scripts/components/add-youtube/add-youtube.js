define(["knockout", "text!./add-youtube.tmpl.html", "knockout-validation", "knockout-postbox"], function (ko, htmlString) {
    // Construct a promise that will be resolved when the Google API is loaded
    var googleApiLoaded = $.Deferred();
    if (typeof (gapi) == "undefined" || typeof (gapi.client) == "undefined" || typeof (gapi.client.request) == "undefined") {
        window.handleGoogleApiLoaded = function () {
            googleApiLoaded.resolve();
        };

        $.getScript("//apis.google.com/js/client.js?onload=handleGoogleApiLoaded");
    } else {
        googleApiLoaded.resolve();
    }

    // Set API key once loaded and then load the YouTube API
    var youTubeDataApiReady = googleApiLoaded.then(function() {
        gapi.client.setApiKey("AIzaSyCGJBnK-sW0Y5IDGdFt8EAVqStNnZ7ZDNw");

        var defer = $.Deferred();
        gapi.client.load("youtube", "v3", function() {
            defer.resolve();
        });
        return defer;
    });

    // ViewModel for adding YouTube video
    function addYouTubeViewModel(params) {
        var self = this;

        // Whether or not the YouTube data API is loaded
        self.youTubeDataApiLoaded = false;

        // When the promise for the page is finished loading, set our view model property to true
        youTubeDataApiReady.done(function () {
            self.youTubeDataApiLoaded = true;
        });

        // The URL of the YouTube video
        self.youTubeUrl = ko.observable("").extend({
            required: true,
            // Custom validator to make sure the YouTube URL gave us a valid location (i.e. has the v= parameter)
            validation: [{
                validator: function(val) {
                    return self.youTubeVideoId();
                },
                message: "Provide a valid YouTube video URL"
            },
            {
                async: true,
                validator: function (val, p, callback) {
                    // If the YouTube API isn't available, just assume OK
                    if (self.youTubeDataApiLoaded === false) {
                        callback(true);
                        return;
                    }
                    
                    // Since this is called whenever the value changes, setup a new deferred each time that can be resolved once
                    // the selection is actually made
                    self.youTubeCallDeferred = $.Deferred();

                    // When the YouTube API call finishes, inspect it to see if we got a valid video
                    self.youTubeCallDeferred.done(function (response) {
                        if (!response || !response.items) {
                            callback({ isValid: false, message: "Unable to validate YouTube video URL" });
                            return;
                        }

                        if (response.items.length !== 1) {
                            callback({ isValid: false, message: "Could not find a YouTube video with that URL" });
                            return;
                        }

                        // Valid
                        callback(true);
                    });
                }
            }]
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

        // The name of the selected YouTube video
        self.youTubeName = ko.observable("").publishOn("add-video-name");

        // The description of the selected YouTube video
        self.youTubeDescription = ko.observable("").publishOn("add-video-description");

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

        // Whether or not we're waiting on data from YouTube while setting a selection
        self.selectionInProgress = ko.observable(false);

        // The call to the YouTube API when the selection is set
        self.youTubeCallDeferred = null;

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
        self.setSelection = function () {
            // Check for any validation problems
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            var youTubeId = self.youTubeVideoId();

            // If no API is available, just go ahead and assume it's a good URL
            if (self.youTubeDataApiLoaded === false) {
                // Set the selection and indicate it's OK to show the common details entry
                self.selectedYouTubeVideoId(youTubeId);
                self.showCommonDetails(true);
                self.savingAvailable(true);
                return;
            }

            // If the YouTube Data API is available, use it to validate the selection and retrieve some details for the video
            self.selectionInProgress(true);
            gapi.client.youtube.videos.list({
                part: "snippet",
                id: youTubeId
            }).execute(function(response) {
                if (response && response.items && response.items.length === 1) {
                    // Use the info from the API to populate name and description
                    self.youTubeName(response.items[0].snippet.title);
                    self.youTubeDescription(response.items[0].snippet.description);

                    // Set the selection, show the rest of the information, and allow saving
                    self.selectedYouTubeVideoId(youTubeId);
                    self.showCommonDetails(true);
                    self.savingAvailable(true);
                }

                // Always resolve the deferred and indicate selection is no longer in progress
                self.youTubeCallDeferred.resolve(response);
                self.selectionInProgress(false);
            });
        };

        // Clears the selected YouTube video
        self.clearSelection = function() {
            self.youTubeUrl("");
            self.youTubeName("");
            self.youTubeDescription("");
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