define(["knockout", "jquery", "text!./uimessages.tmpl.html", "knockout-postbox"], function (ko, $, htmlString) {
    var allUiMessagesQueue = "allmessages";

    // Just once (when this is loaded) hook some global ajax events for jQuery so we can inspect JSON responses
    // and publish any UI messages found on a queue matching the URL for the request
    $(document)
        .ajaxSend(function(event, xhr, settings) {
            // Publish a message on the queue indicating an empty messages array (effectively clearing any messages)
            var queueName = settings.url.toLowerCase();
            ko.postbox.publish(queueName, []);
            ko.postbox.publish(allUiMessagesQueue, []);
        })
        .ajaxSuccess(function(event, xhr, settings) {
            // If there are any UI messages in the response, publish them to the queue for that URL
            if (xhr.responseJSON && xhr.responseJSON.messages.length > 0) {
                var queueName = settings.url.toLowerCase();
                ko.postbox.publish(queueName, xhr.responseJSON.messages);
                ko.postbox.publish(allUiMessagesQueue, xhr.responseJSON.messages);
            }
        })
        .ajaxError(function(event, xhr, settings) {
            // Publish a generic error messages on the queue for the URL
            var queueName = settings.url.toLowerCase();
            ko.postbox.publish(queueName, [{ type: "error", text: "Unexpected server error while processing request.  Please try again later." }]);
            ko.postbox.publish(allUiMessagesQueue, [{ type: "error", text: "Unexpected server error while processing request.  Please try again later." }]);
        });
    
    // Return view model, expecting that we'll get an array of queues to listen to for new messages
    function uiMessagesViewModel(params) {
        var self = this;

        // All messages
        self.messages = ko.observableArray();

        // Messages grouped by type
        self.messagesByType = ko.computed(function() {
            var byType = { success: [], info: [], warning: [], error: [] };
            $.each(self.messages(), function(idx, val) {
                byType[val.type.toLowerCase()].push(val.text);
            });
            return byType;
        });

        var queues = params.queues;
        if (queues) {
            // If queues is just a single queue name, convert it to an array, otherwise make sure all queue name are lowercase
            if ($.isArray(queues) === false) {
                queues = [queues.toLowerCase()];
            } else {
                queues = $.map(queues, function(val, idx) {
                    return val.toLowerCase();
                });
            }

            // Extend the messages array to subscribe to any of the queues specified
            $.each(queues, function(idx, val) {
                self.messages.subscribeTo(val);
            });
        } else {
            // No queue was specified, so assume they want this to subscribe to the "all" queue
            self.messages.subscribeTo(allUiMessagesQueue);
        }
    }

    // Return KO component definition
    return { viewModel: uiMessagesViewModel, template: htmlString };
});