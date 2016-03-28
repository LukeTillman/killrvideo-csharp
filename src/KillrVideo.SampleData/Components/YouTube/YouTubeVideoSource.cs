using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Components.YouTube
{
    /// <summary>
    /// A predefined set of sources for sample YouTube videos.  Use the All field for access to all available sources or one
    /// of the other static fields for access to a specific one.
    /// </summary>
    public abstract class YouTubeVideoSource
    {
        /// <summary>
        /// All available YouTubeVideoSources.
        /// </summary>
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

        // All possible tags from all video sources and other interesting tags
        private static readonly HashSet<string> GlobalTags;
        
        static YouTubeVideoSource()
        {
            All = new List<YouTubeVideoSource>();

            // Seed the global tags list with some interesting keywords/buzzwords that might not fit into local source tags
            GlobalTags = new HashSet<string>
            {
                "asp.net", ".net", "windows 10", "c#", "machine learning", "big data", "tutorial", "beginner", "mvc", "roslyn", "docker",
                "internet of things", "time series", "data model"
            };
            
            // Init all available sources
            PlanetCassandra = new VideosFromChannel("UCvP-AXuCr-naAeEccCfKwUA", new [] { "cassandra", "database", "nosql" });
            DataStaxMedia = new VideosFromChannel("UCqA6zOSMpQ55vvguq4Y0jAg", new[] { "datastax", "cassandra", "database", "nosql" });
            MicrosoftAzure = new VideosFromChannel("UC0m-80FnNY2Qb7obvTL_2fA", new[] { "microsoft", "azure", "cloud", "windows", "linux" });
            MicrosoftCloudPlatform = new VideosFromChannel("UCSgzRJMqIiCNtoM6Q7Q9Lqw", new[] { "microsoft", "azure", "cloud", "windows", "linux" });
            Hanselman = new VideosFromChannel("UCL-fHOdarou-CR2XUmK48Og", new[] { "microsoft", "windows", "linux", "azure" });
            CassandraDatabase = new VideosWithKeyword("cassandra database", new[] { "cassandra", "database", "nosql" });

            FunnyCatVideos = new VideosWithKeyword("funny cat videos", new[] { "cat", "funny" });
            GrumpyCat = new VideosFromChannel("UCTzVrd9ExsI3Zgnlh3_btLg", new[] { "grumpy cat", "funny" });
            MovieTrailers = new VideosFromChannel("UCi8e0iOVk1fEOogdfu4YgfA", new[] { "movie", "trailer", "preview" });
            Snl = new VideosFromChannel("UCqFzWxSCi39LnW1JKFR3efg", new[] { "snl", "saturday night live", "comedy" });
            KeyAndPeele = new VideosFromPlaylist("PL83DDC2327BEB616D", new[] { "key and peele", "comedy" });
        }

        /// <summary>
        /// A unique identifier for the YouTube source.
        /// </summary>
        public abstract string UniqueId { get; }

        /// <summary>
        /// A list of possible tags for videos from this source.  Includes tags specific to this source first, followed by tags from
        /// other sources.
        /// </summary>
        public List<string> PossibleTags
        {
            get { return _possibleTags.Value; }
        }

        /// <summary>
        /// A set of suggested tags just for this video source.
        /// </summary>
        public HashSet<string> SourceTags
        {
            get { return _localTags; }
        }

        private readonly Lazy<List<string>> _possibleTags;
        private readonly HashSet<string> _localTags; 

        protected YouTubeVideoSource(IEnumerable<string> possibleTags)
        {
            _localTags = new HashSet<string>(possibleTags);
            _possibleTags = new Lazy<List<string>>(GetPossibleTags);

            // Add to static list of sources and static collection of tags
            All.Add(this);
            foreach (string tag in _localTags)
                GlobalTags.Add(tag);
        }

        public abstract Task RefreshVideos(SampleYouTubeVideoManager manager);

        private List<string> GetPossibleTags()
        {
            // Start with all tags that are local to this source first
            var tags = new List<string>(_localTags);

            // Then add any tags from the global source (i.e. list of all other sources) that weren't in the local list
            tags.AddRange(GlobalTags.Where(globalTag => _localTags.Contains(globalTag) == false));
            
            return tags;
        }

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

            public VideosFromChannel(string channelId, IEnumerable<string> possibleTags) 
                : base(possibleTags)
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

            public VideosWithKeyword(string searchTerms, IEnumerable<string> possibleTags)
                : base(possibleTags)
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

            public VideosFromPlaylist(string playlistId, IEnumerable<string> possibleTags)
                : base(possibleTags)
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