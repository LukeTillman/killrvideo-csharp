using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KillrVideo.Data.Upload
{
    /// <summary>
    /// Constants/static fields related to video upload.
    /// </summary>
    public static class UploadConfig
    {
        /// <summary>
        /// The queue name used for progress/completion notifications about Azure Media Services encoding jobs.
        /// </summary>
        public const string NotificationQueueName = "video-job-notifications";

        /// <summary>
        /// A prefix to use for the asset name containing the encoded video.
        /// </summary>
        public const string EncodedVideoAssetNamePrefix = "Encoded - ";

        /// <summary>
        /// A prefix to use for the asset name containing the thumbnails. 
        /// </summary>
        public const string ThumbnailAssetNamePrefix = "Thumbnails - ";

        /// <summary>
        /// The max allowed time for a file to be uploaded.
        /// </summary>
        public static readonly TimeSpan UploadMaxTime = TimeSpan.FromHours(8);

        /// <summary>
        /// The set of allowed file extensions that can be uploaded.
        /// </summary>
        public static readonly HashSet<string> AllowedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".3gp",
            ".3g2",
            ".3gp2",
            ".asf",
            ".mts",
            ".m2ts",
            ".avi",
            ".mod",
            ".dv",
            ".ts",
            ".vob",
            ".xesc",
            ".mp4",
            ".mpeg",
            ".mpg",
            ".m2v",
            ".ismv",
            ".wmv"
        };

        /// <summary>
        /// A regex for matching characters that aren't allowed in file names for uploaded files.
        /// </summary>
        public static readonly Regex DisallowedFileNameCharacters = new Regex(@"[\!\*'\(\);:@\&=\+\$,\/\?%#\[\]""\.]",
                                                                              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The XML task definition for generating thumbnails in Azure media services.  (Will generate multiple JPG thumbnails at 480px width).
        /// </summary>
        public const string ThumbnailGenerationXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Thumbnail Size=""480,*"" Type=""Jpeg"" Filename=""{OriginalFilename}_{Size}_{ThumbnailTime}_{ThumbnailIndex}_{Date}_{Time}.{DefaultExtension}"">
<Time Value=""10%"" Step=""10%"" Stop=""95%""/>
</Thumbnail>";
    }
}
