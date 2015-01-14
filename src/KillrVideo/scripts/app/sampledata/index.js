require(["knockout", "jquery", , "lib/knockout-bootstrap-select", "app/common", "app/shared/header"], function (ko, $) {
    function sampleUsersViewModel() {
        var self = this;

        self.dataTypeLabel = "Users";
        self.numberOfUsers = 3;

        self.addSampleData = function() {
            return $.ajax({
                type: "POST",
                url: "/sampledata/addusers",
                data: JSON.stringify({ numberOfUsers: self.numberOfUsers }),
                contentType: "application/json",
                dataType: "json"
            });
        };
    }

    function sampleYouTubeVideosViewModel() {
        var self = this;

        self.dataTypeLabel = "YouTube Videos";
        self.numberOfVideos = 5;

        self.addSampleData = function() {
            return $.ajax({
                type: "POST",
                url: "/sampledata/addyoutubevideos",
                data: JSON.stringify({ numberOfVideos: self.numberOfVideos }),
                contentType: "application/json",
                dataType: "json"
            });
        };
    }

    function sampleCommentsViewModel() {
        var self = this;

        self.dataTypeLabel = "Comments";
        self.numberOfComments = 10;

        self.addSampleData = function() {
            return $.ajax({
                type: "POST",
                url: "/sampledata/addcomments",
                data: JSON.stringify({ numberOfComments: self.numberOfComments }),
                contentType: "application/json",
                dataType: "json"
            });
        };
    }

    function sampleVideoRatingsViewModel() {
        var self = this;

        self.dataTypeLabel = "Video Ratings";

        self.numberOfRatings = 20;

        self.addSampleData = function() {
            return $.ajax({
                type: "POST",
                url: "/sampledata/addvideoratings",
                data: JSON.stringify({ numberOfRatings: self.numberOfRatings }),
                contentType: "application/json",
                dataType: "json"
            });
        }
    }

    function sampleVideoViewsViewModel() {
        var self = this;

        self.dataTypeLabel = "Video Views";

        self.numberOfViews = 100;

        self.addSampleData = function() {
            return $.ajax({
                type: "POST",
                url: "/sampledata/addvideoviews",
                data: JSON.stringify({ numberOfViews: self.numberOfViews }),
                contentType: "application/json",
                dataType: "json"
            });
        };
    }

    // The page view model for adding sample data
    function addSampleDataViewModel() {
        var self = this;

        // The currently selected sample data type
        self.selectedSampleDataType = ko.observable();

        // The model for the selected sample data type
        self.selectedSampleDataTypeModel = ko.computed(function() {
            var selectedType = self.selectedSampleDataType();
            if (!selectedType)
                return null;

            switch (selectedType) {
                case "users":
                    return new sampleUsersViewModel();
                case "youtube":
                    return new sampleYouTubeVideosViewModel();
                case "comments":
                    return new sampleCommentsViewModel();
                case "videoratings":
                    return new sampleVideoRatingsViewModel();
                case "videoviews":
                    return new sampleVideoViewsViewModel();
                default:
                    return null;
            }
        });

        // Whether adding sample data to the site is in progress
        self.saving = ko.observable(false);

        // Whether sample data was successfully added
        self.dataAdded = ko.observable(false);

        // Adds sample data
        self.addSampleData = function () {
            self.dataAdded(false);

            var childModel = self.selectedSampleDataTypeModel();
            if (!childModel)
                return;

            // Indicate we're saving
            self.saving(true);

            // Let the child model save the data
            childModel.addSampleData().then(function(response) {
                if (response.success) {
                    self.dataAdded(true);
                }
            }).always(function() {
                self.saving(false);
            });
        };
    }

    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings(new addSampleDataViewModel(), $("#body-wrapper").get(0));
    });
});