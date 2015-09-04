// Common configuration for RequireJS
requirejs.config({
    baseUrl: "/scripts",
    paths: {
        "jquery": "bower_components/jquery/dist/jquery.min",
        "knockout": "bower_components/knockout/dist/knockout",
        "bootstrap": "bower_components/bootstrap/dist/js/bootstrap.min",
        "text": "bower_components/requirejs-text/text",
        "domReady": "bower_components/requirejs-domReady/domReady",
        "moment": "bower_components/moment/min/moment.min",
        "videojs": "bower_components/videojs/dist/video-js/video",
        "knockout-postbox": "bower_components/knockout-postbox/build/knockout-postbox.min",
        "knockout-validation": "bower_components/knockout-validation/Dist/knockout.validation.min",
        "perfect-scrollbar": "bower_components/perfect-scrollbar/src/perfect-scrollbar",
        "jquery-expander": "bower_components/jquery-expander/jquery.expander",
        "bootstrap-select": "bower_components/bootstrap-select/dist/js/bootstrap-select",
        "bootstrap-tagsinput": "bower_components/bootstrap-tagsinput/dist/bootstrap-tagsinput",
        "arrive": "bower_components/arrive/minified/arrive.min"
    },
    shim: {
        "bootstrap": {
            deps: ["jquery"],
            exports: "$.fn.popover"
        },
        "jquery-expander": ["jquery"],
        "perfect-scrollbar": ["jquery"],
        "bootstrap-select": ["bootstrap"],
        "bootstrap-tagsinput": ["bootstrap"]
    }
});

// Start the app
requirejs(["knockout", "jquery", "require"], function (ko, $, require) {
    // Register some KO components
    var components = [
        "uimessages", "video-preview-list", "user-videos-table", "user-comments-list", "add-upload", "add-youtube", "related-videos",
        "video-upload-status", "video-preview", "view-youtube", "view-upload"
    ];

    for (var i = 0; i < components.length; i++) {
        // Most components should follow this convention
        var name = components[i];
        ko.components.register(name, { require: "components/" + name + "/" + name });
    }

    // Load the model for the current view
    var viewName = $("#requirejs-script").data("view");
    require(["app/shared/header", "app" + viewName], function (headerViewModel, pageViewModel) {
        // Create model instances
        var headerModel = new headerViewModel();
        var pageModel = new pageViewModel();

        // Apply KO bindings on DOM ready
        $(function() {
            ko.applyBindings(headerModel, $("#header").get(0));
            ko.applyBindings(pageModel, $("#body-content").get(0));
        });
    });
});