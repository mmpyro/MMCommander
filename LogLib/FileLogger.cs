using System;
using NLog;

namespace LogLib
{
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
            _logger.Info(ex);
        }

        public void Warn(Exception ex)
        {
            _logger.Warn(ex);
        }

        public void Error(Exception ex)
        {
            _logger.Error(ex);
        }

        public void Info(string message, Exception ex)
        {
            _logger.Info(message, ex);
        }

        public void Warn(string message, Exception ex)
        {
            _logger.Warn(message, ex);
        }

        public void Error(string message, Exception ex)
        {
            _logger.Error(message, ex);
        }

        public event Notify NotifyEvent;
    }
}