require(["knockout", "jquery", "app/common", "app/shared/navbar"], function (ko, $) {
    // Bind the main content area when DOM is ready
    $(function () {
        ko.applyBindings({}, $("#body-wrapper").get(0));
    });
});