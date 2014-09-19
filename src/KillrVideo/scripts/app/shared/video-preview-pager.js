define(["knockout", "jquery", "lib/knockout-extenders"], function (ko, $) {
    /* 
        Return view model.  setupData should be object like this:
            {
                url: '/url/ToLoad/ListFrom', 
                ajaxData: { 
                    'url': 'parameters' 
                },
                pageSize: videosPerPage,
                groupSize: videosPerGroup,
                videoModelConstructor: someFunction
            }
    */
    return function (setupData) {
        var self = this;

        // Merge some defaults into setup data for optional values
        setupData = $.extend({ ajaxData: {}, pageSize: 4, groupSize: 0}, setupData);

        // How many total pages are available
        self.pagesAvailable = ko.observable(1);

        // The current page index (zero based)
        self.currentPage = ko.observable(0);

        // The first video of the next page from the server (will be null initially)
        self.firstVideoOfNextPage = null;

        // All of the videos we've loaded, regardless of page
        self.allVideos = [];
        
        // The list of videos on the current page
        self.videos = ko.computed(function () {
            // Calculate the starting element index in our array of all videos and see if we've already loaded the videos we need
            var allVideosStartIndex = self.currentPage() * setupData.pageSize;
            if (allVideosStartIndex < self.allVideos.length) {
                return self.getCurrentPageSlice();
            }

            // We don't have the videos we need yet, so go to the server to get them

            // Always send over the page size + 1 and the first video of the page if present, combined with any additional data specified
            // in the setupData.ajaxData object
            var ajaxData = $.extend({
                pageSize: setupData.pageSize + 1,       // Always get one more record than we actually need to tell whether there is a next page
                firstVideoOnPage: self.firstVideoOfNextPage
            }, setupData.ajaxData);

            // Return a promise here and use async extender to handle the value while the request is in-flight
            return $.ajax({
                type: "POST",
                url: setupData.url,
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).then(function(response) {
                if (!response.success)
                    return [];

                // See if we got one more record than we actually need
                if (response.data.videos.length === setupData.pageSize + 1) {
                    // Pop the extra record off, store it for next time, and increment the number of pages available
                    self.firstVideoOfNextPage = response.data.videos.pop();
                    self.pagesAvailable(self.pagesAvailable() + 1);
                } else {
                    self.firstVideoOfNextPage = null;
                }

                // Create video preview for each piece of data and add to the allVideos collection
                for (var i = 0; i < response.data.videos.length; i++) {
                    var video = response.data.videos[i];
                    if (setupData.videoModelConstructor) {
                        video = new setupData.videoModelConstructor(video);
                    }
                    self.allVideos.push(video);
                }

                // Return the slice of the allVideos collection
                return self.getCurrentPageSlice();
            });
        }).extend({ async: [] });

        // The list of videos on the current page, grouped into groups of the size specified in setupData
        self.videosGrouped = ko.computed(function () {
            var allVideos = self.videos();
            if (setupData.groupSize === 0 || allVideos.length === 0)
                return allVideos;

            var groups = [];
            var currentGroup = [];
            for (var i = 0; i < allVideos.length; i++) {
                currentGroup.push(allVideos[i]);

                // If we're the last element of the group or the last element of the page, add the group to the list of groups
                if (i % setupData.groupSize === setupData.groupSize - 1 || i === allVideos.length - 1) {
                    groups.push(currentGroup);
                    currentGroup = [];
                }
            }
            
            return groups;
        });

        // Gets the appropriate slice of allVideos for the current page
        self.getCurrentPageSlice = function() {
            var allVideosStartIndex = self.currentPage() * setupData.pageSize;
            var allVideosEndIndex = allVideosStartIndex + setupData.pageSize;
            if (allVideosEndIndex > self.allVideos.length)
                allVideosEndIndex = self.allVideos.length;

            return self.allVideos.slice(allVideosStartIndex, allVideosEndIndex);
        };

        // Whether a next page is available
        self.nextPageAvailable = ko.computed(function() {
            var maxPageIndex = self.pagesAvailable() - 1;
            return self.currentPage() < maxPageIndex;
        });

        // Goes to the next page if available
        self.nextPage = function () {
            // If there isn't a next page available, just bail
            if (self.nextPageAvailable() === false)
                return;

            // Increment the current page index
            self.currentPage(self.currentPage() + 1);
        };

        // Whether a previous page is available
        self.previousPageAvailable = ko.computed(function () {
            return self.currentPage() > 0;
        });

        // Goes to the previous page if available
        self.previousPage = function() {
            // If there isn't a previous page available, just bail
            if (self.previousPageAvailable() === false)
                return;

            // Decrement the current page index
            self.currentPage(self.currentPage() - 1);
        };
    };
});