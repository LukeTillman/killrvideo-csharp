// Common module that does common startup/layout operations
define(["knockout"], function (ko) {
    // Register some KO components
    var components = [
        'uimessages', 'video-preview-list', 'user-videos-table', 'user-comments-list', 'add-upload', 'add-youtube', 'related-videos',
        'video-upload-status'
    ];

    for (var i = 0; i < components.length; i++) {
        // Most components should follow this convention
        var name = components[i];
        ko.components.register(name, { require: 'components/' + name + '/' + name });
    }
});