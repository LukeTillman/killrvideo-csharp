﻿using System;
using Newtonsoft.Json;
namespace KillrVideo.SuggestedVideos.Dtos
{
    /// <summary>
    /// DTO representing some basic details of a video necessary for showing a preview.
    /// </summary>
    [Serializable]
    public class VideoPreview
    {
        public Guid VideoId { get; set; }
        public DateTimeOffset AddedDate { get; set; }
        public string Name { get; set; }
        [JsonProperty("preview_image_location")]
        public string PreviewImageLocation { get; set; }
        public Guid UserId { get; set; }
    }
}