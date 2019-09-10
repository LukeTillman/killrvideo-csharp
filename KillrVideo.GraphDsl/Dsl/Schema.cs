using System;
using System.Collections.Generic;

namespace KillrVideo.GraphDsl {

    /// <summary>
    /// String tokens for graph element lables and property keys.
    /// </summary>
    public static class KvGraph {

        // Vertices labels
        public const String VertexUser           = "user";
        public const String VertexVideo          = "video";
        public const String VertexTag            = "tag";

        // Edges types
        public const String EdgeRated            = "rated";
        public const String EdgeUploaded         = "uploaded";
        public const String EdgeTagWith          = "taggedWith";

        // Properties (both Vertices and edges)
        public const String PropertyRating       = "rating";
        public const String PropertyVideoId      = "videoId";
        public const String PropertyUserId       = "userId";
        public const String PropertyName         = "name";
        public const String PropertyAddedDate    = "added_date";
        public const String PropertyPreviewImage = "preview_image_location";

        // Meta
        public const String KeyVertex            = "_vertex";
        public const String KeyInDegree          = "";
        public const String KeyOutDegree         = "";
        public const String KeyDegree            = "";
        public const String KeyDistribution      = "";
    }


}