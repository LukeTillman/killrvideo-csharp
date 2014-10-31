define(["knockout", "text!./add-upload.tmpl.html", "knockout-validation", "knockout-postbox", "lib/knockout-file-drop"], function(ko, htmlString) {
    // ViewModel for adding an uploaded video
    function addUploadViewModel(params) {
        var self = this;

        var maxFileSize = 1073741824,       // Max upload size of 1 GB
            defaultChunkSize = 1024 * 512;  // Upload in 512 KB chunks; 

        // Whether or not upload is supported (must support HTML5 File APIs)
        self.uploadSupported = window.File && window.FileReader && window.FileList && window.Blob;

        // The file to be uploaded
        self.uploadFile = ko.observable().extend({
            required: true,
            validation: {
                validator: function (val) {
                    return val ? val.size <= maxFileSize : false;
                },
                message: "Upload file cannot be larger than 1 GB"
            }
        });

        // A display label for the selected file (i.e. the file name)
        self.uploadFileLabel = ko.pureComputed(function() {
            var f = self.uploadFile();
            return f ? f.name : "No file selected.";
        });

        // The asset data for the upload file (set once upload is complete)
        self.uploadFileAssetData = ko.observable();

        // How far along we are in the upload
        self.percentComplete = ko.observable(-1);

        // A status message for updates during an upload
        self.uploadStatus = ko.observable("");

        // Whether or not the upload has been cancelled by the user
        self.uploadCancelled = ko.observable(true);

        // Whether or not a failed upload can be retried
        self.uploadRetryAvailable = ko.observable(false);

        // Whether it's OK to show the rest of the common details form entry
        self.showCommonDetails = ko.observable(false).syncWith("add-video-showCommonDetails", false, false);

        // Any validation errors
        self.validationErrors = ko.validation.group([self.uploadFile]);

        // Starts the upload when a file is selected
        self.startUpload = ko.computed(function() {
            var uploadFile = self.uploadFile();
            if (!uploadFile) return;

            // Make sure we're valid
            if (self.validationErrors().length > 0) {
                self.validationErrors.showAllMessages();
                return;
            }

            // Start the upload (use a computed that we immediately dispose of to avoid capturing any dependencies
            // from the doUpload method)
            ko.computed(self.doUpload).dispose();
            self.showCommonDetails(true);
        });

        // Does the actual file upload
        self.doUpload = function() {
            var uploadFile = self.uploadFile();

            // Indicate an upload is about to start
            self.percentComplete(0);
            self.uploadStatus("Preparing to upload video");
            self.uploadCancelled(false);
            self.uploadRetryAvailable(false);

            // Create the asset on our server, then upload to Azure
            createAsset(uploadFile).then(uploadTheFile)
                .done(function() {
                    // One last check to make sure we didn't get cancelled
                    if (self.uploadCancelled() === true)
                        return $.Deferred().reject().promise();

                    self.uploadStatus("Upload completed successfully");
                    self.savingAvailable(true);
                })
                .fail(function() {
                    // If the upload was cancelled, just reset the selection
                    if (self.uploadCancelled() === true) {
                        self.resetFile();
                        return;
                    }

                    // Otherwise there was some server error, so allow retry
                    self.uploadStatus("Error while uploading, click retry to try again");
                    self.uploadRetryAvailable(true);
                });
        };

        // Cancels an in progress upload
        self.cancelUpload = function() {
            self.uploadCancelled(true);
            self.uploadStatus("Cancelling upload");
        };

        // Resets the selected file
        self.resetFile = function () {
            self.uploadFile(null);
            self.uploadFileAssetData(null);
            self.percentComplete(-1);
            self.uploadStatus("");
            self.showCommonDetails(false);
            self.savingAvailable(false);
            self.validationErrors.showAllMessages(false);
        };

        // Whether or not we're saving
        self.saving = ko.observable(false).syncWith("add-video-saving");

        // Whether or not saving is available
        self.savingAvailable = ko.observable(false).syncWith("add-video-savingAvailable");

        // The URL where the newly added video can be viewed
        self.viewVideoUrl = ko.observable("").publishOn("add-video-viewVideoUrl");

        // Subscribe to the save video click on the parent and save the video
        ko.postbox.subscribe("add-upload-save-clicked", function (videoDetails) {
            var createAssetData = self.uploadFileAssetData();

            var addVideoData = {
                // Copy some data from the response of the create asset call
                assetId: createAssetData.assetId,
                fileName: createAssetData.fileName,
                uploadLocatorId: createAssetData.uploadLocatorId,

                // Copy some data from when save was originally called
                name: videoDetails.name,
                description: videoDetails.description,
                tags: videoDetails.tags
            };

            // Add the video
            $.post("/upload/add", addVideoData)
                .then(function(response) {
                    // If there was some problem, just bail
                    if (!response.success)
                        return;

                    // Indicate the URL where the video can be viewed
                    self.viewVideoUrl(response.data.viewVideoUrl);
                })
                .always(function() {
                    self.saving(false);
                });
        });

        // Handle disposal
        self.dispose = function () {
            // Cancel any in-progress uploads
            self.cancelUpload();
        };

        // Creates the upload file asset in Azure Media Services via a call to our web app
        function createAsset(uploadFile) {
            // Make sure we weren't cancelled yet
            if (self.uploadCancelled() === true)
                return $.Deferred().reject().promise();

            return $.post("/upload/createasset", { fileName: uploadFile.name })
                .then(function(createAssetResponse) {
                    // If server response indicates failure or the upload was cancelled, just return a rejected promise
                    if (!createAssetResponse.success || self.uploadCancelled() === true)
                        return $.Deferred().reject().promise();

                    return $.Deferred().resolve(uploadFile, createAssetResponse).promise();
                });
        }

        // Does the file upload directly to Azure storage (in chunks)
        function uploadTheFile(uploadFile, createAssetResponse) {
            // Make sure we weren't cancelled yet
            if (self.uploadCancelled() === true)
                return $.Deferred().reject().promise();

            // Init some variables for file upload
            var chunks = Math.ceil(uploadFile.size / defaultChunkSize),
                defer = $.Deferred(),
                uploadFileChunks = defer;

            // Chain tasks to read each chunk of the file, then upload via a PUT to Azure
            for (var i = 0; i < chunks; i++) {
                uploadFileChunks = uploadFileChunks.then(readFileChunk).then(putFileChunk);
            }

            // Once all chunks have been uploaded, we need to make a final request to commit the block list
            uploadFileChunks = uploadFileChunks.then(putBlockList);

            // Upload is done, so chain one more task to save the asset data
            uploadFileChunks = uploadFileChunks.then(function() {
                return self.uploadFileAssetData(createAssetResponse.data);
            });

            // Create a file reader and resolve the original deferred we created with it to start the upload
            var fileReader = new FileReader();
            defer.resolve(uploadFile, fileReader, 0, chunks, [], createAssetResponse.data.uploadUrl);   // These args will be passed to the first readFileChunk call

            // Return the finished upload process as a promise
            return uploadFileChunks.promise();
        }

        // Reads a chunk of the file to upload and returns a promise that can be chained to the putFileChunk method
        function readFileChunk(uploadFile, fr, currentChunk, chunks, blockIds, uploadUrl) {
            // Make sure we weren't cancelled yet
            if (self.uploadCancelled() === true)
                return $.Deferred().reject().promise();

            // Create a deferred that will be resolved once the file read is done
            var frDeferred = $.Deferred();
            fr.onloadend = function (e) {
                if (e.target.readyState == FileReader.DONE) {
                    // If read was successful, resolve with args needed by putFileChunk method
                    frDeferred.resolve(uploadFile, e, currentChunk, chunks, blockIds, uploadUrl);
                } else {
                    frDeferred.reject(e);
                }
            };

            // Figure out where to slice the file
            var start = currentChunk * defaultChunkSize;
            var end = start + defaultChunkSize >= uploadFile.size ? uploadFile.size : start + defaultChunkSize;

            // Read the chunk of the file
            fr.readAsArrayBuffer(uploadFile.slice(start, end));

            return frDeferred;
        }

        // Puts the file chunk that was read in Azure storage
        function putFileChunk(uploadFile, frLoadEndEvent, currentChunk, chunks, blockIds, uploadUrl) {
            // Make sure we weren't cancelled yet
            if (self.uploadCancelled() === true)
                return $.Deferred().reject().promise();

            // Generate a block Id for the chunk by padding 0s before the current chunk number
            var blockId = "" + currentChunk;
            while (blockId.length < 10) {
                blockId = "0" + blockId;
            }
            blockId = btoa("block-" + blockId); // Base-64 encode
            blockIds.push(blockId);

            self.uploadStatus("Uploading (part " + (currentChunk + 1) + " of " + chunks + ")");

            // When a chunk is done being read by the FileReader, make a PUT request to Azure Storage to store it
            var uri = uploadUrl + "&comp=block&blockid=" + blockId;
            var requestData = new Uint8Array(frLoadEndEvent.target.result);

            return $.ajax({
                url: uri,
                type: "PUT",
                headers: { 'x-ms-blob-type': 'BlockBlob' },
                data: requestData,
                processData: false
            }).then(function (putResponse) {
                // Make sure we weren't cancelled yet
                if (self.uploadCancelled() === true)
                    return $.Deferred().reject().promise();
                
                // Keep track of progress
                currentChunk++;
                var percentComplete = ((parseFloat(currentChunk) / parseFloat(chunks)) * 100).toFixed(2);
                self.percentComplete(percentComplete);

                // Return the arguments needed by readFileChunk so another chunk can possibly be chained
                return $.Deferred().resolve(uploadFile, frLoadEndEvent.target, currentChunk, chunks, blockIds, uploadUrl).promise();
            });
        }

        // Puts the block list for the uploaded file in Azure storage
        function putBlockList(uploadFile, fr, currentChunk, chunks, blockIds, uploadUrl) {
            // Make sure we weren't cancelled yet
            if (self.uploadCancelled() === true)
                return $.Deferred().reject().promise();

            self.uploadStatus("Finishing upload");

            var uri = uploadUrl + "&comp=blocklist";

            var requestBody = "<?xml version='1.0' encoding='utf-8'?><BlockList>";
            for (var j = 0; j < blockIds.length; j++) {
                requestBody += "<Latest>" + blockIds[j] + "</Latest>";
            }
            requestBody += "</BlockList>";

            return $.ajax({
                url: uri,
                type: "PUT",
                headers: { "x-ms-blob-content-type": uploadFile.type },
                data: requestBody
            });
        }
    };

    // Return KO component definition
    return { viewModel: addUploadViewModel, template: htmlString };
});