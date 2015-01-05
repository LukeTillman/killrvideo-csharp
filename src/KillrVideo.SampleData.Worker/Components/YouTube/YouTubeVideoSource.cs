using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.SampleData.Worker.Components.YouTube
{
    /// <summary>
    /// A predefined set of sources for sample YouTube videos.  Use the All field for access to all available sources or one
    /// of the other static fields for access to a specific one.
    /// </summary>
    public abstract class YouTubeVideoSource
    {
        public static readonly List<YouTubeVideoSource> All;
 
        // Tech related
        public static readonly YouTubeVideoSource PlanetCassandra;
        public static readonly YouTubeVideoSource DataStaxMedia;
        public static readonly YouTubeVideoSource MicrosoftAzure;
        public static readonly YouTubeVideoSource MicrosoftCloudPlatform;
        public static readonly YouTubeVideoSource Hanselman;
        public static readonly YouTubeVideoSource CassandraDatabase;

        // Random sources
        public static readonly YouTubeVideoSource FunnyCatVideos;
        public static readonly YouTubeVideoSource GrumpyCat;
        public static readonly YouTubeVideoSource MovieTrailers;
        public static readonly YouTubeVideoSource Snl;
        public static readonly YouTubeVideoSource KeyAndPeele;

        static YouTubeVideoSource()
        {
            All = new List<YouTubeVideoSource>();

            // Init all available sources
            PlanetCassandra = new VideosFromChannel("UCvP-AXuCr-naAeEccCfKwUA");
            DataStaxMedia = new VideosFromChannel("UCqA6zOSMpQ55vvguq4Y0jAg");
            MicrosoftAzure = new VideosFromChannel("UC0m-80FnNY2Qb7obvTL_2fA");
            MicrosoftCloudPlatform = new VideosFromChannel("UCSgzRJMqIiCNtoM6Q7Q9Lqw");
            Hanselman = new VideosFromChannel("UCL-fHOdarou-CR2XUmK48Og");
            CassandraDatabase = new VideosWithKeyword("cassandra database");

            FunnyCatVideos = new VideosWithKeyword("funny cat videos");
            GrumpyCat = new VideosFromChannel("UCTzVrd9ExsI3Zgnlh3_btLg");
            MovieTrailers = new VideosFromChannel("UCi8e0iOVk1fEOogdfu4YgfA");
            Snl = new VideosFromChannel("UCqFzWxSCi39LnW1JKFR3efg");
            KeyAndPeele = new VideosFromPlaylist("PL83DDC2327BEB616D");
        }

        public abstract string UniqueId { get; }

        protected YouTubeVideoSource()
        {
            All.Add(this);
        }

        public abstract Task RefreshVideos(SampleYouTubeVideoManager manager);

        /// <summary>
        /// Searches for videos in the channel with the Id specified in the constructor.
        /// </summary>
        internal class VideosFromChannel : YouTubeVideoSource
        {
            private readonly string _channelId;
            
            public override string UniqueId
            {
                get { return _channelId; }
            }

            public string ChannelId
            {
                get { return _channelId; }
            }

            public VideosFromChannel(string channelId)
            {
                if (channelId == null) throw new ArgumentNullException("channelId");
                _channelId = channelId;
            }

            public override Task RefreshVideos(SampleYouTubeVideoManager manager)
            {
                return manager.RefreshChannel(this);
            }
        }

        /// <summary>
        /// Searches videos by the keyword specified in the constructor.
        /// </summary>
        internal class VideosWithKeyword : YouTubeVideoSource
        {
            private readonly string _searchTerms;

            public override string UniqueId
            {
                get { return _searchTerms; }
            }

            public string SearchTerms
            {
                get { return _searchTerms; }
            }

            public VideosWithKeyword(string searchTerms)
            {
                if (searchTerms == null) throw new ArgumentNullException("searchTerms");
                _searchTerms = searchTerms;
            }

            public override Task RefreshVideos(SampleYouTubeVideoManager manager)
            {
                return manager.RefreshKeywords(this);
            }
        }

        /// <summary>
        /// Gets videos from a playlist with the id specified in the constructor.
        /// </summary>
        internal class VideosFromPlaylist : YouTubeVideoSource
        {
            private readonly string _playlistId;
            
            public override string UniqueId
            {
                get { return _playlistId; }
            }

            public string PlaylistId
            {
                get { return _playlistId; }
            }

            public VideosFromPlaylist(string playlistId)
            {
                if (playlistId == null) throw new ArgumentNullException("playlistId");
                _playlistId = playlistId;
            }

            public override Task RefreshVideos(SampleYouTubeVideoManager manager)
            {
                return manager.RefreshPlaylist(this);
            }
        }
    }
}