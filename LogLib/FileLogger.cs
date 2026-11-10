using System;
using NLog;

namespace LogLib
{
    // Exceptions are passed as NLog's exception argument so the layout (NLog.config) writes the full stack trace.
    public class FileLogger : ILogger
    {
        private readonly Logger _logger;

        public FileLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(Exception ex)
        {
            _logger.Info(ex, ex.Message);
        }

        public void Warn(Exception ex)
        {
            _logger.Warn(ex, ex.Message);
        }

        public void Error(Exception ex)
        {
            _logger.Error(ex, ex.Message);
        }

        public void Info(string message, Exception ex)
        {
            _logger.Info(ex,message);
        }

        public void Warn(string message, Exception ex)
        {
            _logger.Warn(ex, message);
        }

        public void Error(string message, Exception ex)
        {
            _logger.Error(ex, message);
        }

        public void Fatal(Exception ex)
        {
            _logger.Fatal(ex, ex.Message);
        }

        public event Notify NotifyEvent;
    }
}