require(["knockout", "jquery", "app/common", "app/shared/header"], function (ko, $) {
    // View model for the search results page
    function searchResultsModel(query) {
        var self = this;

        var allVideos = [],
            ajaxData = {
                query: query,
                pageSize: 8,
                pagingState: null
            },
            lastPageLoaded = -1;

        // Whether or not a page is being loaded
        self.isLoading = ko.observable(false);

        // Whether or not our request for a next page loaded an empty page
        self.loadedEmptyPage = ko.observable(false);

        // The current page index (zero based)
        self.currentPage = ko.observable(-1);

        // The videos on the current page
        self.videos = ko.pureComputed(function() {
            // Figure out the start and end indexes for our current page slice, making sure we don't overflow the end of the array
            var startIdx = self.currentPage() * ajaxData.pageSize;
            var endIdx = startIdx + ajaxData.pageSize;
            if (endIdx > allVideos.length)
                endIdx = allVideos.length;

            // Get the slice of records
            return allVideos.slice(startIdx, endIdx);
        });

        // Whether a previous page is available
        self.previousPageAvailable = ko.pureComputed(function() {
            return self.currentPage() > 0;
        });

        // Go to the previous page if available
        self.previousPage = function() {
            var currentPage = self.currentPage();
            if (currentPage > 0) {
                self.currentPage(currentPage - 1);
                self.loadedEmptyPage(false);
            }
        };

        // Whether a next page is available
        self.nextPageAvailable = ko.pureComputed(function() {
            var loadedEmptyPage = self.loadedEmptyPage();
            if (loadedEmptyPage) return false;

            var currentPage = self.currentPage();

            // If we're on the last page that was loaded from the server, we only have more pages if the
            // server gave us paging state for the next page
            if (currentPage === lastPageLoaded)
                return ajaxData.pagingState !== null;

            return currentPage < lastPageLoaded;
        });

        // Go to the next page if available
        self.nextPage = function () {
            // See if we already have data for the next page
            var nextPage = self.currentPage() + 1;
            if (nextPage <= lastPageLoaded) {
                self.currentPage(nextPage);
                return;
            }

            // Need to load from the server
            self.isLoading(true);

            $.ajax({
                type: "POST",
                url: "/search/videos",
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).then(function (response) {
                if (!response.success)
                    return;

                // Save paging state for possible next page request
                ajaxData.pagingState = response.data.pagingState;

                // It's possible to get back an empty page if we're out of search results but the server gave us a paging state
                if (lastPageLoaded >= 0 && response.data.videos.length === 0) {
                    self.loadedEmptyPage(true);
                    return;
                }

                // Add videos to the all videos array
                for (var i = 0; i < response.data.videos.length; i++) {
                    var video = response.data.videos[i];
                    allVideos.push(video);
                }
                
                // Change the current page and increment the pages we've loaded from the server
                lastPageLoaded++;
                self.currentPage(nextPage);
            }).always(function () {
                // Always indicate we're finished loading
                self.isLoading(false);
            });
        };

        // Get the initial page on creation
        self.nextPage();
    }

    // Bind the main content area when DOM is ready
    $(function () {
        // Include the query that was searched for
        var query = $("#searched-for").val();
        ko.applyBindings(new searchResultsModel(query), $("#body-content").get(0));
    });
});