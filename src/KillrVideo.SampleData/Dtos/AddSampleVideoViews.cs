using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    [Serializable]
    public class AddSampleVideoViews : IBusCommand
    {
        public int NumberOfViews { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}