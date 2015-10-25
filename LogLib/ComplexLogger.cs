using System;
using System.Collections.Generic;
using System.Linq;

namespace LogLib
{
    public class ComplexLogger : ILogger
    {
        private readonly List<ILogger> _loggers;

        public ComplexLogger(params ILogger[] loggers)
        {
            _loggers = loggers.ToList();
        }

        public void Info(string message)
        {
            _loggers.ForEach(t => t.Info(message));
        }

        public void Warn(string message)
        {
            _loggers.ForEach(t => t.Warn(message));
        }

        public void Error(string message)
        {
            _loggers.ForEach(t => t.Error(message));
        }

        public void Debug(string message)
        {
            _loggers.ForEach(t => t.Debug(message));
        }

        public void Info(Exception ex)
        {
            _loggers.ForEach(t => t.Info(ex));
        }

        public void Warn(Exception ex)
        {
            _loggers.ForEach(t => t.Warn(ex));
        }

        public void Error(Exception ex)
        {
            _loggers.ForEach(t => t.Error(ex));
        }

        public void Info(string message, Exception ex)
        {
            _loggers.ForEach(t => t.Info(message, ex));
        }

        public void Warn(string message, Exception ex)
        {
            _loggers.ForEach(t => t.Warn(message, ex));
        }

        public void Error(string message, Exception ex)
        {
            _loggers.ForEach(t => t.Error(message, ex));
        }

        public event Notify NotifyEvent
        {
            add
            {
                _loggers.ForEach(t => t.NotifyEvent += value);
            }
            remove
            {
                _loggers.ForEach(t => t.NotifyEvent -= value);
            }
        }
    }
}