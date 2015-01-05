using System;
using Nimbus.MessageContracts;

namespace KillrVideo.SampleData.Dtos
{
    [Serializable]
    public class AddSampleComments : IBusCommand
    {
        public int NumberOfComments { get; set; }
    }
}