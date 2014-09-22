require(["knockout", "jquery", "videojs", "app/common", "app/shared/header"], function (ko, $) {
    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings({}, $("#body-wrapper").get(0));
    });
});