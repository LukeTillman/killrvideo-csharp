using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    [Serializable]
    public class AddSampleRatings : IBusCommand
    {
        public int NumberOfRatings { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}