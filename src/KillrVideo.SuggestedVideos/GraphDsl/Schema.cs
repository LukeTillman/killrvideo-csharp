using System;
using System.Collections.Generic;

namespace KillrVideo.SuggestedVideos.GraphDsl {

    /// <summary>
    /// String tokens for graph element lables and property keys.
    /// </summary>
    public static class Kv {

        public const String VertexMovie = "movie";
        public const String VertexPerson = "person";
        public const String VertexUser = "user";
        public const String VertexGenre = "genre";

        public const String EdgeActor = "actor";
        public const String EdgeRated = "rated";
        public const String EdgeBelongsTo = "belongsTo";

        public const String KeyAge = "age";
        public const String KeyCountry = "country";
        public const String KeyDegree = "_degree";
        public const String KeyDuration = "duration";
        public const String KeyInDegree = "_inDegree";
        public const String KeyVideoId = "videoId";
        public const String KeyName = "name";
        public const String KeyOutDegree = "_outDegree";
        public const String KeyDistribution = "_distribution";
        public const String KeyPersonId = "personId";
        public const String KeyProduction = "production";
        public const String KeyRating = "rating";
        public const String KeyTitle = "title";
        public const String KeyUserId = "userId";
        public const String KeyVertex = "_vertex";
        public const String KeyYear = "year";
        public const String KeyAddedDate = "added_date";
        public const String KeyPreviewImage = "preview_image_location";

    }

    /// <summary>
    /// The available "genre" vertex types in the KillrVideo dataset.
    /// </summary>
    public enum Genre {
        Action, Adventure,Animation,
        Comedy, Documentary, Drama,
        Fantasy, FilmNoir, Horror,
        Kids, Musical, Mystery,
        Romance, SciFi, TvSeries,
        Thriller, War, Western
    }

    public static class GenreLookup
    {
        public static readonly Dictionary<Genre, string> Names = new Dictionary<Genre, string>
            {
                {Genre.Action, "Action"},
                {Genre.Adventure, "Adventure"},
                {Genre.Animation, "Animation"},
                {Genre.Comedy, "Comedy"},
                {Genre.Documentary, "Documentary"},
                {Genre.Drama, "Drama"},
                {Genre.Fantasy, "Fantasy"},
                {Genre.FilmNoir, "Film-Noir"},
                {Genre.Horror, "Horror"},
                {Genre.Kids, "Kids"},
                {Genre.Musical, "Musical"},
                {Genre.Mystery, "Mystery"},
                {Genre.Romance, "Romance"},
                {Genre.SciFi, "Sci-Fi"},
                {Genre.TvSeries, "TV Series"},
                {Genre.Thriller, "Thriller"},
                {Genre.War, "War"},
                {Genre.Western, "Western"}
            };
    }

}