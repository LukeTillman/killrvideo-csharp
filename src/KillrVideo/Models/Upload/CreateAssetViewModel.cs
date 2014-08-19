using System;

namespace KillrVideo.Models.Upload
{
    /// <summary>
    /// ViewModel for requesting creation of a new Azure Media Services asset for an uploaded video.
    /// </summary>
    [Serializable]
    public class CreateAssetViewModel
    {
        public string FileName { get; set; }
    }
}