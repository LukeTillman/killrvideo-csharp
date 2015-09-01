define(["jquery", "knockout", "text!./video-preview-list.tmpl.html"], function ($, ko, htmlString) {
    function videoPreviewListModel(params) {
        var self = this;

        var allVideos = [],
            ajaxData = {
                pageSize: 5,    // Get 5 records at a time since we show 4 + 1 from next page
                pagingState: null
            };

        // Merge in any AJAX parameters provided to the constructor
        if (params.ajaxData) {
            $.extend(ajaxData, params.ajaxData);
        }

        // Whether or not a page is being loaded
        self.isLoading = ko.observable(false);

        // The number of records to show per page (not counting the preview video for the next page)
        self.pageSize = 4;

        // Whether or not our request for a next page loaded an empty page
        self.loadedEmptyPage = ko.observable(false);

        // The current page index (zero based)
        self.currentPage = ko.observable(-1);

        // The videos on the current page
        self.videos = ko.pureComputed(function () {
            // Figure out the start and end indexes for our current page slice, making sure we don't overflow the end of the array
            var startIdx = self.currentPage() * self.pageSize;
            var endIdx = startIdx + self.pageSize;
            if (endIdx > allVideos.length)
                endIdx = allVideos.length;

            // Get the slice of records
            return allVideos.slice(startIdx, endIdx);
        });

        // The first video on the next page
        self.firstVideoOfNextPage = ko.pureComputed(function() {
            // Figure out the array index of the first video on the next page
            var idx = (self.currentPage() + 1) * self.pageSize;

            // If that index exists in the all videos array, return the video, otherwise return null
            return idx < allVideos.length ? allVideos[idx] : null;
        });

        // Whether a previous page is available
        self.previousPageAvailable = ko.pureComputed(function () {
            return self.currentPage() > 0;
        });

        // Go to the previous page if available
        self.previousPage = function () {
            var currentPage = self.currentPage();
            if (currentPage > 0) {
                self.currentPage(currentPage - 1);
                self.loadedEmptyPage(false);
            }
        };

        // Whether a next page is available
        self.nextPageAvailable = ko.pureComputed(function () {
            return self.firstVideoOfNextPage() !== null;
        });

        // Go to the next page if available
        self.nextPage = function () {
            // See if we already have all the data we need in the allVideos array
            var nextPageNumber = self.currentPage() + 1;
            var lastIdxShown = (nextPageNumber + 1) * self.pageSize;
            if (lastIdxShown <= allVideos.length) {
                self.currentPage(nextPageNumber);
                return;
            }

            // Need to load from the server
            self.isLoading(true);

            $.ajax({
                type: "POST",
                url: params.url,
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).then(function (response) {
                if (!response.success)
                    return;

                // Save paging state for possible next page request
                ajaxData.pagingState = response.data.pagingState;

                // It's possible to get back an empty page if we're out of search results but the server gave us a paging state
                if (nextPageNumber > 0 && response.data.videos.length === 0) {
                    self.loadedEmptyPage(true);
                    return;
                }

                // Add videos to the all videos array
                for (var i = 0; i < response.data.videos.length; i++) {
                    var video = response.data.videos[i];
                    allVideos.push(video);
                }

                // Change the current page and increment the pages we've loaded from the server
                self.currentPage(nextPageNumber);
            }).always(function () {
                // Always indicate we're finished loading
                self.isLoading(false);
            });
        };

        // Get the initial page of data on creation
        self.nextPage();
    }

    // Return a KO component definition
    return { viewModel: videoPreviewListModel, template: htmlString };
});