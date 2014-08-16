using System.Diagnostics;
using log4net.Appender;
using log4net.Core;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Log4net appender that logs using trace at the appropriate level.
    /// </summary>
    public class AzureTraceAppender : TraceAppender
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            Level level = loggingEvent.Level;
            string message = RenderLoggingEvent(loggingEvent);

            if (level >= Level.Fatal)
            {
                Trace.Write(message, "Critical");
            }
            else if (level >= Level.Error)
            {
                Trace.TraceError(message);
            }
            else if (level >= Level.Warn)
            {
                Trace.TraceWarning(message);
            }
            else if (level >= Level.Info)
            {
                Trace.TraceInformation(message);
            }
            else
            {
                Trace.Write(message);
            }

            if (ImmediateFlush)
            {
                Trace.Flush();
            }
        }
    }
}