// Knockout binding that takes turns an element into one that support file selection via drag and drop
define(["knockout", "jquery"], function (ko, $) {
    function fileDrop(element, allowMultiple) {
        this.disabled = true;
        this.element = $(element);
        this.allowMultiple = allowMultiple;
        
        // Add a hidden file input after the element
        var fileEl = document.createElement("input");
        fileEl.setAttribute("type", "file");
        fileEl.style.display = "none";
        if (allowMultiple) {
            fileEl.setAttribute("multiple", "multiple");
        }
        $(element).after(fileEl);
        this.fileElement = $(fileEl);

        // Start enabled by default
        this.enable();
    }

    // Disables the fileDrop UI elements
    fileDrop.prototype.disable = function () {
        // Just bail if already disabled
        if (this.disabled)
            return;

        // Remove the event handlers
        this.element.off(".fileDrop");
        this.fileElement.off(".fileDrop");

        // Add class to element
        this.element.addClass("file-drop-enabled");

        this.disabled = true;
    };

    // Enables the fileDrop UI elements
    fileDrop.prototype.enable = function () {
        // Just bail if already enabled
        if (this.disabled === false)
            return;

        var self = this;

        // Add the event handlers
        this.element.on({
            "click.fileDrop": function (e) {
                // Trigger the click event on the hidden file input
                self.fileElement.click();
                e.preventDefault();
            },
            "dragenter.fileDrop": function (e) {
                self.element.addClass("file-drop-draghover");
                e.stopPropagation();
                e.preventDefault();
            },
            "dragover.fileDrop": function(e) {
                e.stopPropagation();
                e.preventDefault();
            },
            "dragleave.fileDrop": function (e) {
                self.element.removeClass("file-drop-draghover");
                e.stopPropagation();
                e.preventDefault();
            },
            "drop.fileDrop": function (e) {
                e.stopPropagation();
                e.preventDefault();

                self.element.removeClass("file-drop-draghover");
                self.filesselected(e.originalEvent.dataTransfer.files);
            }
        });

        this.fileElement.on({
            "change.fileDrop": function (e) {
                self.filesselected(e.currentTarget.files);

                // Reset the file input so that if the user uses it to select the same file again, this
                // event will still be fired
                self.fileElement.wrap("<form>").closest("form").get(0).reset();
                self.fileElement.unwrap();
            }
        });

        // Remove class from element
        $(this.element).removeClass("file-drop-disabled");

        this.disabled = false;
    };

    // Fires the filesselected event with the files specified
    fileDrop.prototype.filesselected = function(files) {
        if (this.allowMultiple) {
            this.element.trigger("filesselected", files);
        } else {
            this.element.trigger("filesselected", files[0]);
        }
    };

    // Destroys the fileDrop instance
    fileDrop.prototype.destroy = function() {
        // Remove the event handlers
        this.element.off(".fileDrop");
        this.fileElement.off(".fileDrop");
    };

    // Add a custom binding to KO
    ko.bindingHandlers.fileDrop = {
        after: ["enable", "disable"],
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var val = valueAccessor();

            // If the observable we're binding to is an array, allow multiple selections
            var unwrapped = ko.unwrap(val);
            var allowMultiple = unwrapped && typeof unwrapped.length === "number" ? true : false;

            // Create the file drop instance 
            var fd = new fileDrop(element, allowMultiple);

            // When a file is selected, update the observable
            $(element).on({
                "filesselected" : function(e, f) {
                    if (allowMultiple) {
                        val.push(f);
                    } else {
                        val(f);
                    }
                }
            });

            // Store the fileDrop instance in the element's data collection
            $(element).data("fileDrop", fd);

            // If the node is torn down, call destroy on the fileDrop instance
            ko.utils.domNodeDisposal.addDisposeCallback(element, function () {
                var fdInstance = $(element).data("fileDrop");
                fdInstance.destroy();
            });
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            var fd = $(element).data("fileDrop");

            if (allBindings.has("disable")) {
                var disabled = ko.unwrap(allBindings.get("disable"));
                if (disabled) {
                    fd.disable();
                } else {
                    fd.enable();
                }
            }

            if (allBindings.has("enable")) {
                var enabled = ko.unwrapKey(allBindings.get("enable"));
                if (enabled) {
                    fd.enable();
                } else {
                    fd.disable();
                }
            }
        }
    };
});