using System;
using System.Collections.Generic;

namespace KillrVideo.Uploads.Worker.EncodingJobMonitor
{
    /// <summary>
    /// Represents an event message pulled down from a notificaiton queue.
    /// </summary>
    [Serializable]
    public class EncodingJobEvent
    {
        public string MessageVersion { get; set; }
        public string EventType { get; set; }
        public string ETag { get; set; }
        public DateTime TimeStamp { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public int RetryAttempts { get; set; }

        public bool IsJobStateChangeEvent()
        {
            return EventType == "JobStateChange";
        }

        public string GetJobId()
        {
            return Properties["JobId"];
        }

        public string GetNewState()
        {
            return Properties["NewState"];
        }

        public string GetOldState()
        {
            return Properties["OldState"];
        }

        public bool IsJobFinished()
        {
            string newState = GetNewState();
            return newState == "Canceled" || newState == "Error" || newState == "Finished";
        }

        public bool WasSuccessful()
        {
            return GetNewState() == "Finished";
        }
    }
}