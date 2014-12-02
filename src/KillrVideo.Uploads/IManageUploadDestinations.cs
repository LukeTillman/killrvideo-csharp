using System.Threading.Tasks;
using KillrVideo.Uploads.Messages.RequestResponse;

namespace KillrVideo.Uploads
{
    public interface IManageUploadDestinations
    {
        /// <summary>
        /// Generates a URL where a video file can be uploaded.
        /// </summary>
        Task<UploadDestination> GenerateUploadDestination(GenerateUploadDestination request);

        /// <summary>
        /// Marks an upload as completed successfully.
        /// </summary>
        Task MarkUploadComplete(string uploadUrl);
    }
}
