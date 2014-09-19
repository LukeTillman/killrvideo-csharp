define(["knockout", "text!./add-upload.tmpl.html", "knockout-validation"], function(ko, htmlString) {
    // ViewModel for adding an uploaded video
    function addUploadViewModel(params) {
        var self = this;

        var maxFileSize = 1073741824; // Max upload size of 1 GB

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
        self.uploadFileLabel = ko.computed(function() {
            var f = self.uploadFile();
            return f ? f.name : "No file selected.";
        });

        // Handles "change" event for the selected file to upload input
        self.handleFileChange = function (data, event) {
            self.uploadFile(event.currentTarget.files[0]);
        };

        // How far along we are in the upload
        self.percentComplete = ko.observable(-1);

        // A status message for updates during an upload
        self.uploadStatus = ko.observable("");

        // Required method for all video sources, this does the actual work of uploading the file and eventually returning a
        // location where the video can be viewed
        self.saveVideo = function(videoDetails) {
            var uploadFile = self.uploadFile();

            // Indicate an upload is about to start
            self.percentComplete(0);
            self.uploadStatus("Preparing to upload video.");

            // Start by creating an asset on the server and getting a URL/Id for it
            var promise =
                $.post("/upload/createasset", { fileName: uploadFile.name })
                    // After the asset has been created, upload the file
                    .then(function(createAssetResponse) {
                        // If server response indicates failure, just return a rejected promise
                        if (!createAssetResponse.success)
                            return $.Deferred().reject().promise();

                        // Init some variables for file upload
                        var defaultChunkSize = 1024 * 512, // Upload in 512 KB chunks
                            chunks = Math.ceil(uploadFile.size / defaultChunkSize),
                            currentChunk = 0,
                            blockIds = [],
                            defer = $.Deferred(),
                            uploadFileChunks = defer;

                        // Chain tasks to read each chunk of the file, then upload via a PUT to Azure
                        for (var i = 0; i < chunks; i++) {
                            uploadFileChunks = uploadFileChunks.then(function(fr) {
                                // Create a deferred that will be resolved once the file read is done
                                var frDeferred = $.Deferred();
                                fr.onloadend = function(e) {
                                    if (e.target.readyState == FileReader.DONE) {
                                        frDeferred.resolve(e);
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
                            }).then(function(e) {
                                // Generate a block Id for the chunk by padding 0s before the current chunk number
                                var blockId = "" + currentChunk;
                                while (blockId.length < 10) {
                                    blockId = "0" + blockId;
                                }
                                blockId = btoa("block-" + blockId); // Base-64 encode
                                blockIds.push(blockId);

                                self.uploadStatus("Uploading (part " + (currentChunk + 1) + " of " + chunks + ")");

                                // When a chunk is done being read by the FileReader, make a PUT request to Azure Storage to store it
                                var uri = createAssetResponse.data.uploadUrl + "&comp=block&blockid=" + blockId;
                                var requestData = new Uint8Array(e.target.result);

                                return $.ajax({
                                    url: uri,
                                    type: "PUT",
                                    headers: { 'x-ms-blob-type': 'BlockBlob' },
                                    data: requestData,
                                    processData: false
                                }).then(function(putResponse) {
                                    // Keep track of progress
                                    currentChunk++;
                                    var percentComplete = ((parseFloat(currentChunk) / parseFloat(chunks)) * 100).toFixed(2);
                                    self.percentComplete(percentComplete);

                                    // Return the FileReader so another chunk can possibly be chained
                                    return e.target;
                                });
                            });
                        }

                        // Once all chunks have been uploaded, we need to make a final request to commit the block list
                        uploadFileChunks = uploadFileChunks.then(function() {
                            self.uploadStatus("Finishing upload.");

                            var uri = createAssetResponse.data.uploadUrl + "&comp=blocklist";

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
                        });

                        // Lastly, return the data from the create asset call above
                        uploadFileChunks = uploadFileChunks.then(function() {
                            return createAssetResponse.data;
                        });

                        // Create a file reader and resolve the original deferred we created with it to start the upload
                        var fileReader = new FileReader();
                        defer.resolve(fileReader);

                        // Return the finished upload process as a promise
                        return uploadFileChunks.promise();

                        // After the file has been uploaded, add the video
                    }).then(function(createAssetData) {
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
                        return $.post("/upload/add", addVideoData);
                    }).then(function(addResponse) {
                        // If server response indicates failure, just return a rejected promise
                        if (!addResponse.success)
                            return $.Deferred().reject().promise();

                        // Return the URL where the video will be available for viewing
                        return addResponse.data.viewVideoUrl;
                    });

            // If something goes wrong during upload, reset the upload progress to -1
            promise.fail(function() {
                self.percentComplete(-1);
            });

            return promise;
        };

        // Pass self back to parent
        params.selectedSourceModel(self);
    };

    // Return KO component definition
    return { viewModel: addUploadViewModel, template: htmlString };
});