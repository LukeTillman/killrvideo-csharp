define(["jquery", "app/shared/video-preview-pager", "text!./video-preview-list.tmpl.html"], function ($, videoPagerModel, htmlString) {
    // Return a KO component definition
    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                // Merge params data into object that specifies some settings needed by this component
                var setupData = $.extend(params, { pageSize: 4 });

                // Create an instance of the shared view model with some parameters set
                return new videoPagerModel(setupData);
            }
        },
        template: htmlString
    };
});