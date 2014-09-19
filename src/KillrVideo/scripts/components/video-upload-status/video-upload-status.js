define(["knockout", "jquery", "moment", "text!./video-upload-status.tmpl.html"], function(ko, $, moment, htmlString) {
    function uploadStatusViewModel(params) {
        var self = this;

        self.status = ko.observable("");
        self.statusDateFormatted = ko.observable("");

        self.isComplete = ko.computed(function () {
            return self.status() == "Finished";
        });

        self.isErrored = ko.computed(function () {
            var s = self.status();
            return s == "Canceled" || s == "Error";
        });

        self.isInProgress = ko.computed(function () {
            return !self.isComplete() && !self.isErrored();
        });

        // Loads the latest status for the job from the server
        function loadLatestStatus() {
            $.ajax({
                type: "POST",
                url: "/upload/getlateststatus",
                data: JSON.stringify({ jobId: params.jobId }),
                contentType: "application/json",
                dataType: "json"
            }).done(function (response) {
                if (!response.success)
                    return;

                // Update view model with latest data
                self.status(response.data.status);
                self.statusDateFormatted(moment(response.data.statusDate).fromNow());
            }).always(function () {
                // If job is still in progress, refresh again in 20 seconds
                if (self.isInProgress()) {
                    setTimeout(loadLatestStatus, 20000);
                }
            });
        };

        // Load the latest status for the job
        loadLatestStatus();
    }

    // Return KO component definition
    return { viewModel: uploadStatusViewModel, template: htmlString };
});