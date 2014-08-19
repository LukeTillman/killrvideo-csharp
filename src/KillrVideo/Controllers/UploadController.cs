using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Models.Upload;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller handles upload of videos.
    /// </summary>
    public class UploadController : ConventionControllerBase
    {
        private const string UploadAccessPolicyName = "Video Upload Access Policy";
        private const int UploadMaxHours = 8;
        private const string UploadLocatorName = "Upload Locator";

        private readonly CloudMediaContext _cloudMediaContext;

        public UploadController(CloudMediaContext cloudMediaContext)
        {
            if (cloudMediaContext == null) throw new ArgumentNullException("cloudMediaContext");
            _cloudMediaContext = cloudMediaContext;
        }

        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonNetResult> CreateAsset(CreateAssetViewModel model)
        {
            // TODO:  Validate file type?

            // Create the media services asset
            string assetName = string.Format("Original - {0}", model.FileName);
            IAsset asset = await _cloudMediaContext.Assets.CreateAsync(assetName, AssetCreationOptions.None, CancellationToken.None);
            IAssetFile file = await asset.AssetFiles.CreateAsync(model.FileName, CancellationToken.None);
            
            // Create locator for the upload directly to storage
            IAccessPolicy writePolicy = await GetUploadAccessPolicy();
            ILocator uploadLocator = await _cloudMediaContext.Locators.CreateSasLocatorAsync(asset, writePolicy, DateTime.UtcNow.AddMinutes(-2),
                                                                                             UploadLocatorName);

            var uploadUrl = new UriBuilder(uploadLocator.Path);
            uploadUrl.Path = uploadUrl.Path + "/" + model.FileName;

            // Return the Id and the URL where the file can be uploaded
            return JsonSuccess(new AssetCreatedViewModel { AssetId = asset.Id, UploadUrl = uploadUrl.Uri.AbsoluteUri });
        }

        [HttpPost]
        public async Task<JsonNetResult> PublishAsset(PublishAssetViewModel model)
        {
            // TODO:  Publish
            return JsonFailure();
        }

        private async Task<IAccessPolicy> GetUploadAccessPolicy()
        {
            // TODO:  Can this be created once and then just reused instead of querying every time?

            // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
            IAccessPolicy accessPolicy = _cloudMediaContext.AccessPolicies.Where(p => p.Name == UploadAccessPolicyName).FirstOrDefault();
            if (accessPolicy != null)
                return accessPolicy;

            return await _cloudMediaContext.AccessPolicies.CreateAsync(UploadAccessPolicyName, TimeSpan.FromHours(UploadMaxHours),
                                                                       AccessPermissions.Write);
        }
    }
}