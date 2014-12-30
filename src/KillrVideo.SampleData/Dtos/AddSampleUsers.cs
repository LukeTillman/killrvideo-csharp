using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    [Serializable]
    public class AddSampleUsers : IBusCommand
    {
        public int NumberOfUsers { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
