namespace KillrVideo.SuggestedVideos
{
    /// <summary>
    /// Options needed by the Video Suggestions service implementations.
    /// </summary>
    public class SuggestionsOptions
    {
        /// <summary>
        /// Whether or not suggestions via DSE are enabled.
        /// </summary>
        public bool DseEnabled { get; set; }
    }
}
