using System;
using System.Collections.Generic;

namespace KillrVideo.Models.Shared
{
    /// <summary>
    /// A generic return result model for JSON responses.
    /// </summary>
    [Serializable]
    public class JsonResultViewModel
    {
        public bool Success { get; set; }
        public List<UiMessageViewModel> Messages { get; set; }
        public object Data { get; set; }

        public JsonResultViewModel()
        {
            Messages = new List<UiMessageViewModel>();
        }
    }
}