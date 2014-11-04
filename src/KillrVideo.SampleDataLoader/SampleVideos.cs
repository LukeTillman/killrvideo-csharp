using System;
using System.Collections.Generic;
using KillrVideo.Data;
using KillrVideo.Data.Videos.Dtos;

namespace KillrVideo.SampleDataLoader
{
    /// <summary>
    /// Contains sample video data for populating the KillrVideo schema.
    /// </summary>
    public static class SampleVideos
    {
        public static readonly AddVideo[] Data =
        {
            new AddVideo
            {
                UserId = SampleUsers.Data[0].UserId,
                VideoId = Guid.NewGuid(),
                Name = "Marvel's Guardians of the Galaxy - New Trailer Teaser 1",
                Description = "",
                Location = "wN10QltBBtE",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "comic books", "marvel", "movie", "trailer" },
                PreviewImageLocation = "//img.youtube.com/vi/wN10QltBBtE/hqdefault.jpg",
            },
            new AddVideo
            {
                UserId = SampleUsers.Data[0].UserId,
                VideoId = Guid.NewGuid(),
                Name = "WHEN WILL THE BASS DROP? (ft. Lil Jon)",
                Description = "",
                Location = "XCawU6BE8P8",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "comedy", "music", "snl" },
                PreviewImageLocation = "//img.youtube.com/vi/XCawU6BE8P8/hqdefault.jpg",
            },
            new AddVideo
            {
                UserId = SampleUsers.Data[0].UserId,
                VideoId = Guid.NewGuid(),
                Name = "Bunny Eating Raspberries!",
                Description = "",
                Location = "A9HV5O8Un6k",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "animal", "bunny", "food", "funny" },
                PreviewImageLocation = "//img.youtube.com/vi/A9HV5O8Un6k/hqdefault.jpg",
            },
            new AddVideo
            {
                UserId = SampleUsers.Data[0].UserId,
                VideoId = Guid.NewGuid(),
                Name = "Tiny hamster eating a tiny pizza",
                Description = "",
                Location = "FNf-IGmxElI",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "animal", "food", "funny", "hamster" },
                PreviewImageLocation = "//img.youtube.com/vi/FNf-IGmxElI/hqdefault.jpg",
            },
            new AddVideo
            {
                UserId = SampleUsers.Data[1].UserId,
                VideoId = Guid.NewGuid(),
                Name = "Last Week Tonight with John Oliver (HBO): Net Neutrality",
                Description = "",
                Location = "fpbOEoRrHyU",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "comedy", "hbo", "net neutrality" },
                PreviewImageLocation = "//img.youtube.com/vi/fpbOEoRrHyU/hqdefault.jpg",
            },
            new AddVideo
            {
                UserId = SampleUsers.Data[1].UserId,
                VideoId = Guid.NewGuid(),
                Name = "Before They Were On Game Of Thrones",
                Description = "",
                Location = "rct8l4_ezJs",
                LocationType = VideoLocationType.YouTube,
                Tags = new HashSet<string> { "got", "grrm", "hbo" },
                PreviewImageLocation = "//img.youtube.com/vi/rct8l4_ezJs/hqdefault.jpg",
            },
        };
    }
}
