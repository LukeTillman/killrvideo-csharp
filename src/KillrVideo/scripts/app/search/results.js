require(["knockout", "jquery", "app/shared/video-preview-pager", "app/common", "app/shared/header"], function (ko, $, videoPreviewPagerModel) {
    // Bind the main content area when DOM is ready
    $(function () {
        // Include the query that was searched for in the ajaxData
        var query = $("#searched-for").val();

        // Just use a simple object as the model for the page and apply bindings
        var pageModel = {
            searchResultsList: new videoPreviewPagerModel({
                url: '/search/videos',
                ajaxData: {
                    query: query
                },
                pageSize: 8
            })
        };

        ko.applyBindings(pageModel, $("#body-wrapper").get(0));
    });
});