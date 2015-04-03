using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KillrVideo.Uploads.Worker
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
        public static readonly string ThumbnailGenerationXml;

        /// <summary>
        /// The XML task definition for generating re-encoded videos in Azure Media Services.  Is a modified version of the "H264 Broadband SD 16x9"
        /// preset with automatic rotation detection enabled to allow for videos shot in portrait mode on a mobile device to be rotated.
        /// </summary>
        ///  <see cref="http://msdn.microsoft.com/en-us/library/dn619405.aspx">H264 Broadband SD 16x9</see>
        /// <see cref="http://azure.microsoft.com/blog/2014/08/21/advanced-encoding-features-in-azure-media-encoder/">Advanced Encoding Features</see>
        public static readonly string VideoGenerationXml;

        static UploadConfig()
        {
            // Load the two XML strings from the embedded resources
            ThumbnailGenerationXml = ReadEmbeddedResource("ThumbnailGenerationTask.xml");
            VideoGenerationXml = ReadEmbeddedResource("VideoGenerationTask.xml");
        }

        private static string ReadEmbeddedResource(string resourceFileName)
        {
            var asm = typeof(UploadConfig).Assembly;
            var resourceNamespace = string.Format("{0}.Resources", asm.GetName().Name);

            string resourceName = string.Format("{0}.{1}", resourceNamespace, resourceFileName);
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(string.Format("Cannot load resource {0}", resourceName));

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
