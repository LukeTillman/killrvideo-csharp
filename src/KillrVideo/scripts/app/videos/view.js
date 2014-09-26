require(["knockout", "jquery", "videojs", "lib/knockout-perfect-scrollbar", "app/common", "app/shared/header"], function (ko, $, rateModel) {
    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings({}, $("#body-wrapper").get(0));
    });
});