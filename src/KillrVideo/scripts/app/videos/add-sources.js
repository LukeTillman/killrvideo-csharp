define(["knockout", "app/videos/add-upload", "app/videos/add-youtube"], function (ko, uploadModel, youTubeModel) {
    return [
        { label: "Upload a Video", model: new uploadModel(), template: "app/videos/add-upload" },
        { label: "YouTube", model: new youTubeModel(), template: "app/videos/add-youtube" }
    ];
});