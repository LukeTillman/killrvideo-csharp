define(["knockout", "knockout-validation"], function(ko) {
    // Review ViewModel for adding an uploaded video
    return function() {
        var self = this;

        // The file to be uploaded and a label for it
        self.uploadFile = ko.observable("").extend({ required: true });
        self.uploadFileName = ko.computed(function() {
            var f = self.uploadFile();
            return f ? f : "No file selected.";
        });

        
        self.location = ko.observable("");
        self.locationType = ko.observable("upload");
    };
});