using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.InternalEvents;
using KillrVideo.Uploads.Messages.RequestResponse;
using KillrVideo.Utils;
using Microsoft.WindowsAzure.MediaServices.Client;
using Nimbus;

namespace KillrVideo.Uploads
{
    /// <summary>
    /// Component responsible for managing upload destinations in Azure Media Services.
    /// </summary>
    public class UploadDestinationManager : IManageUploadDestinations
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;
        private readonly CloudMediaContext _cloudMediaContext;

        public UploadDestinationManager(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus,
                                        CloudMediaContext cloudMediaContext)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (bus == null) throw new ArgumentNullException("bus");
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");

            _session = session;
            _statementCache = statementCache;
            _bus = bus;
            _cloudMediaContext = cloudMediaContext;
        }

        /// <summary>
        /// Generates a URL where a video file can be uploaded.
        /// </summary>
        public async Task<UploadDestination> GenerateUploadDestination(GenerateUploadDestination request)
        {
            // Validate the file extension is one supported by media services and sanitize the file name to remove any invalid characters
            string fileName;
            if (TryVerifyAndSanitizeFileName(request.FileName, out fileName) == false)
                return new UploadDestination { ErrorMessage = "That file type is not currently supported." };

            // Create the media services asset
            string assetName = string.Format("Original - {0}", fileName);
            IAsset asset = await _cloudMediaContext.Assets.CreateAsync(assetName, AssetCreationOptions.None, CancellationToken.None);
            IAssetFile file = await asset.AssetFiles.CreateAsync(fileName, CancellationToken.None);

            // Create locator for the upload directly to storage
            ILocator uploadLocator = await _cloudMediaContext.Locators.CreateAsync(LocatorType.Sas, asset, AccessPermissions.Write,
                                                                                   UploadConfig.UploadMaxTime, DateTime.UtcNow.AddMinutes(-2));

            var uploadUrl = new UriBuilder(uploadLocator.Path);
            uploadUrl.Path = uploadUrl.Path + "/" + fileName;
            string absoluteUploadUrl = uploadUrl.Uri.AbsoluteUri;

            // Store some of that information in Cassandra so we can look it up later
            PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                "INSERT INTO uploaded_video_destinations (upload_url, assetid, filename, locatorid) VALUES (?, ?, ?, ?)");
            BoundStatement bound = prepared.Bind(absoluteUploadUrl, asset.Id, fileName, uploadLocator.Id);
            bound.SetTimestamp(DateTime.UtcNow);
            await _session.ExecuteAsync(bound);

            // Let everyone know we added an upload destination
            await _bus.Publish(new UploadDestinationAdded { UploadUrl = absoluteUploadUrl, Timestamp = bound.Timestamp.Value });

            // Reply
            return new UploadDestination { ErrorMessage = null, UploadUrl = absoluteUploadUrl };
        }

        // ReSharper disable ReplaceWithSingleCallToFirstOrDefault

        /// <summary>
        /// Marks an upload as completed successfully.
        /// </summary>
        public async Task MarkUploadComplete(string uploadUrl)
        {
            // Find the details for the upload destination based on the Upload Url
            PreparedStatement getPrepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM uploaded_video_destinations WHERE upload_url = ?");
            RowSet rows = await _session.ExecuteAsync(getPrepared.Bind(uploadUrl));
            Row uploadDestination = rows.SingleOrDefault();
            if (uploadDestination == null)
                throw new InvalidOperationException(string.Format("Could not find upload destination details for URL {0}", uploadUrl));

            var assetId = uploadDestination.GetValue<string>("assetid");
            var filename = uploadDestination.GetValue<string>("filename");
            var locatorId = uploadDestination.GetValue<string>("locatorid");

            // Find the asset to be published
            IAsset asset = _cloudMediaContext.Assets.Where(a => a.Id == assetId).FirstOrDefault();
            if (asset == null)
                throw new InvalidOperationException(string.Format("Could not find asset {0}.", assetId));

            // Set the file as the primary asset file
            IAssetFile assetFile = asset.AssetFiles.Where(f => f.Name == filename).FirstOrDefault();
            if (assetFile == null)
                throw new InvalidOperationException(string.Format("Could not find file {0} on asset {1}.", filename, assetId));

            assetFile.IsPrimary = true;
            await assetFile.UpdateAsync();

            // Remove the upload locator (i.e. revoke upload access)
            ILocator uploadLocator = asset.Locators.Where(l => l.Id == locatorId).FirstOrDefault();
            if (uploadLocator != null)
                await uploadLocator.DeleteAsync();

            // Tell the world an upload finished
            await _bus.Publish(new UploadCompleted
            {
                AssetId = assetId,
                Filename = filename
            });
        }

        // ReSharper restore ReplaceWithSingleCallToFirstOrDefault

        /// <summary>
        /// Verifies the file is an allowed file extension type and sanitizes the file name if the extension type is allowed.
        /// </summary>
        private static bool TryVerifyAndSanitizeFileName(string fileName, out string sanitizedFileName)
        {
            sanitizedFileName = null;

            if (string.IsNullOrEmpty(fileName))
                return false;

            // Verify the file extension is allowed
            string extension = Path.GetExtension(fileName);
            if (UploadConfig.AllowedFileExtensions.Contains(extension) == false)
                return false;

            // Remove any disallowed characters in the file name (including any extra "." since only 1 for the extension is allowed)
            sanitizedFileName = Path.GetFileNameWithoutExtension(fileName);
            sanitizedFileName = UploadConfig.DisallowedFileNameCharacters.Replace(sanitizedFileName, string.Empty);
            sanitizedFileName = string.Format("{0}.{1}", sanitizedFileName, extension);
            return true;
        }
    }
}