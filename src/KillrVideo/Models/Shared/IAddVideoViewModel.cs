namespace KillrVideo.Models.Shared
{
    public interface IAddVideoViewModel
    {
        /// <summary>
        /// The name/title of the video.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The description for the video.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Any tags for the video, as a comma-delimited string.
        /// </summary>
        string Tags { get; set; }
    }
}
