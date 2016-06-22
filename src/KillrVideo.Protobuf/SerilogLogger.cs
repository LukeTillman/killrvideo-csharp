using System;
using Grpc.Core.Logging;

namespace KillrVideo.Protobuf
{
    /// <summary>
    /// Wrapper for using Serilog to log Grpc messages.
    /// </summary>
    internal class SerilogGrpcLogger : ILogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogGrpcLogger(Serilog.ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        public ILogger ForType<T>()
        {
            return new SerilogGrpcLogger(_logger.ForContext<T>());
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Debug(string format, params object[] formatArgs)
        {
            _logger.Debug(string.Format(format, formatArgs));
        }

        public void Info(string message)
        {
            _logger.Information(message);
        }

        public void Info(string format, params object[] formatArgs)
        {
            _logger.Information(string.Format(format, formatArgs));
        }

        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        public void Warning(string format, params object[] formatArgs)
        {
            _logger.Warning(string.Format(format, formatArgs));
        }

        public void Warning(Exception exception, string message)
        {
            _logger.Warning(exception, message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string format, params object[] formatArgs)
        {
            _logger.Error(string.Format(format, formatArgs));
        }

        public void Error(Exception exception, string message)
        {
            _logger.Error(exception, message);
        }
    }
}
