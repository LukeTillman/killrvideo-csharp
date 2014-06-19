using System;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// DTO for renaming a video.
    /// </summary>
    [Serializable]
    public class RenameVideo
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
    }
}