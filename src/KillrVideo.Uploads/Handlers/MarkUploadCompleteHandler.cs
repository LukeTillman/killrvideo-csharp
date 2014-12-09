using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Uploads.InternalEvents;
using KillrVideo.Uploads.Messages.Commands;
using KillrVideo.Utils;
using Microsoft.WindowsAzure.MediaServices.Client;
using Nimbus;
using Nimbus.Handlers;

namespace KillrVideo.Uploads.Handlers
{
    /// <summary>
    /// Marks uploads as complete.
    /// </summary>
    public class MarkUploadCompleteHandler : IHandleCommand<MarkUploadComplete>
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly IBus _bus;
        private readonly CloudMediaContext _cloudMediaContext;

        public MarkUploadCompleteHandler(ISession session, TaskCache<string, PreparedStatement> statementCache, IBus bus, CloudMediaContext cloudMediaContext)
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

        // ReSharper disable ReplaceWithSingleCallToFirstOrDefault

        public async Task Handle(MarkUploadComplete markComplete)
        {
            // Find the details for the upload destination based on the Upload Url
            PreparedStatement getPrepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT * FROM uploaded_video_destinations WHERE upload_url = ?");
            RowSet rows = await _session.ExecuteAsync(getPrepared.Bind(markComplete.UploadUrl));
            Row uploadDestination = rows.SingleOrDefault();
            if (uploadDestination == null)
                throw new InvalidOperationException(string.Format("Could not find upload destination details for URL {0}", markComplete.UploadUrl));

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
    }
}