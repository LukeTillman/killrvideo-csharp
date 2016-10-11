using System;

namespace KillrVideo.Search.Dtos
{
    [Serializable]
    public class Suggestion
    {
        public string Term { get; set; }
        public int Weight { get; set; }
        public string Payload { get; set; }
    }
}