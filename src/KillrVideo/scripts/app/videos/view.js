require(["knockout", "jquery", "videojs", "lib/knockout-perfect-scrollbar", "lib/knockout-expander", "app/common", "app/shared/header"], function (ko, $) {
    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings({}, $("#body-wrapper").get(0));
    });
});