// Common configuration for RequireJS
var require = {
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
    },
    shim: {
        "bootstrap": {
            deps: ["jquery"],
            exports: "$.fn.popover"
        },
        "jquery-expander": ["jquery"]
    }
};