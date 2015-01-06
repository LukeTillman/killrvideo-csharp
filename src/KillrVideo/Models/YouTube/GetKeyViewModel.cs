using System;

namespace KillrVideo.Models.YouTube
{
    /// <summary>
    /// The return view model for getting the YouTube API key from the server.
    /// </summary>
    [Serializable]
    public class GetKeyViewModel
    {
        public string YouTubeApiKey { get; set; }
    }
}