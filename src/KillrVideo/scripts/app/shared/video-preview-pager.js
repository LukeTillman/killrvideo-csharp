define(["knockout", "jquery"], function (ko, $) {
    /* 
        Return view model.  setupData should be object like this:
            {
                url: '/url/ToLoad/ListFrom', 
                ajaxData: { 
                    'url': 'parameters' 
                },
                pageSize: videosPerPage,
                videoModelConstructor: someFunction
            }
    */
    return function (setupData) {
        var self = this;

        // Merge some defaults into setup data for optional values
        setupData = $.extend({ ajaxData: {}, pageSize: 4 }, setupData);

        // Keep track of all videos we've loaded, regardless of page, as well as the last page we loaded from the server
        var allVideos = [],
            lastPageIndexLoaded = -1;

        // The current page index (zero based)
        self.currentPage = ko.observable(-1);

        // The number of video previews per page
        self.pageSize = setupData.pageSize;
        
        // The first video of the next page from the server (will be null initially)
        self.firstVideoOfNextPage = ko.pureComputed(function () {
            // Figure out the array index of the first video on the next page
            var idx = (self.currentPage() + 1) * setupData.pageSize;

            // If that index exists in the all videos array, return the video, otherwise return null
            var video = idx < allVideos.length ? allVideos[idx] : null;
            return video !== null && setupData.videoModelConstructor ? new setupData.videoModelConstructor(video) : video;
        });

        // The list of videos on the current page
        self.videos = ko.pureComputed(function () {
            // Figure out the start and end indexes for our current page slice, making sure we don't overflow the end of the array
            var startIdx = self.currentPage() * setupData.pageSize;
            var endIdx = startIdx + setupData.pageSize;
            if (endIdx > allVideos.length)
                endIdx = allVideos.length;

            // Get the slice of records
            var videos = allVideos.slice(startIdx, endIdx);
            if (!setupData.videoModelConstructor)
                return videos;

            // A view model constructor for individual videos was present, so return an array of those objects
            return ko.utils.arrayMap(videos, function(data) {
                return new setupData.videoModelConstructor(data);
            });
        });

        // Whether or not we're currently loading data from the server
        self.isLoading = ko.observable(false);
        
        // Whether a next page is available
        self.nextPageAvailable = ko.pureComputed(function () {
            return self.firstVideoOfNextPage() != null;
        });

        // Goes to the next page if available
        self.nextPage = function () {
            var currentPage = self.currentPage();

            // If there isn't a next page available, just bail
            var firstVideoOfNextPageIndex = (currentPage + 1) * setupData.pageSize;
            if (currentPage >= 0 && allVideos.length < firstVideoOfNextPageIndex)
                return;
            
            // See if we've already loaded the videos we need for the next page from the server
            var pageToLoad = currentPage + 1;
            if (pageToLoad <= lastPageIndexLoaded) {
                self.currentPage(pageToLoad);
                return;
            }

            // We haven't gone to the server for that page yet, so go get the page
            self.isLoading(true);

            // Always send over the page size + 1 and the first video of the page if present, combined with any additional data specified
            // in the setupData.ajaxData object
            var ajaxData = $.extend({
                pageSize: setupData.pageSize + 1,       // Always get one more record than we actually need to tell whether there is a next page
                firstVideoOnPage: allVideos[firstVideoOfNextPageIndex]
            }, setupData.ajaxData);
            
            $.ajax({
                type: "POST",
                url: setupData.url,
                data: JSON.stringify(ajaxData),
                contentType: "application/json",
                dataType: "json"
            }).then(function (response) {
                if (!response.success)
                    return;
                
                // Create video preview for each piece of data and add to the allVideos collection
                for (var i = 0; i < response.data.videos.length; i++) {
                    var video = response.data.videos[i];
                    if (i === 0 && allVideos.length > 0) {
                        // After the initial load, the first record should always replace the last record since they should be the same video preview
                        allVideos[allVideos.length - 1] = video;
                    } else {
                        allVideos.push(video);
                    }
                }

                // Change the current page and increment the pages we've loaded from the server
                lastPageIndexLoaded++;
                self.currentPage(pageToLoad);
            }).always(function () {
                // Always indicate we're finished loading
                self.isLoading(false);
            });
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

        // Load the first page
        self.nextPage();
    };
});