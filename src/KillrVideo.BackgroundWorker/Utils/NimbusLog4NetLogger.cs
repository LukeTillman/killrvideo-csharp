using System;
using log4net;
using Nimbus;

namespace KillrVideo.BackgroundWorker.Utils
{
    /// <summary>
    /// Nimbus logger that uses a log4net logger.
    /// </summary>
    public class NimbusLog4NetLogger : ILogger
    {
        private static readonly ILog Logger;
        private static readonly bool IsDebugEnabled;
        private static readonly bool IsInfoEnabled;
        private static readonly bool IsWarnEnabled;
        private static readonly bool IsErrorEnabled;
        
        static NimbusLog4NetLogger()
        {
            Logger = LogManager.GetLogger("Nimbus");
            IsDebugEnabled = Logger.IsDebugEnabled;
            IsInfoEnabled = Logger.IsInfoEnabled;
            IsWarnEnabled = Logger.IsWarnEnabled;
            IsErrorEnabled = Logger.IsErrorEnabled;
        }

        public void Debug(string format, params object[] args)
        {
            if (IsDebugEnabled)
                Logger.DebugFormat(format, args);
        }

        public void Info(string format, params object[] args)
        {
            if (IsInfoEnabled)
                Logger.InfoFormat(format, args);
        }

        public void Warn(string format, params object[] args)
        {
            if (IsWarnEnabled)
                Logger.WarnFormat(format, args);
        }

        public void Error(string format, params object[] args)
        {
            if (IsErrorEnabled)
                Logger.ErrorFormat(format, args);
        }

        public void Error(Exception exc, string format, params object[] args)
        {
            if (IsErrorEnabled)
                Logger.Error(string.Format(format, args), exc);
        }
    }
}
