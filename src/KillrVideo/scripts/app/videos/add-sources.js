define(["knockout", "app/videos/add-youtube"], function (ko, youTubeModel) {
    return [
        { label: "YouTube", model: new youTubeModel(), template: "app/videos/add-youtube" }
    ];
});