using System;

namespace KillrVideo.Data.Videos.Dtos
{
    /// <summary>
    /// DTO for change a video description.
    /// </summary>
    [Serializable]
    public class ChangeVideoDescription
    {
        public Guid VideoId { get; set; }
        public string Description { get; set; }
    }
}