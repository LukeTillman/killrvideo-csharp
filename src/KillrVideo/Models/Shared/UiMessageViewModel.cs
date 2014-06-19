using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KillrVideo.Models.Shared
{
    [Serializable]
    public class UiMessageViewModel
    {
        #region MessageType enum

        public enum MessageType
        {
            Success = 0,
            Information = 1,
            Warning = 2,
            Error = 3
        }

        #endregion

        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType Type { get; private set; }

        public string Text { get; private set; }

        public UiMessageViewModel(MessageType messageType, string messageText)
        {
            if (messageText == null) throw new ArgumentNullException("messageText");
            
            Type = messageType;
            Text = messageText;
        }

        public UiMessageViewModel(MessageType messageType, string messageTextFormatString, params object[] formatArgs)
            : this(messageType, string.Format(messageTextFormatString, formatArgs))
        {
        }
    }
}