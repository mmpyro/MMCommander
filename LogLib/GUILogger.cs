using System;

namespace LogLib
{
    public class GUILogger : ILogger
    {
        private readonly LogLevel _logLevel;
        public delegate void Notify(LogInfo info, string message);
        public event Notify NotifyEvent;

        private string PerformMessage(string message)
        {
            DateTime dt = DateTime.Now;
            return string.Format("{0}|{1}",dt.ToShortTimeString() , message);
        }

        private string PerformMessage(Exception ex)
        {
            DateTime dt = DateTime.Now;
            return string.Format("{0}|{1}", dt.ToShortTimeString(), ex.Message);
        }

        public GUILogger(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public void WriteLine(string message, LogInfo info, LogLevel level)
        {
            if (_logLevel >= level && NotifyEvent != null)
                NotifyEvent(info, PerformMessage(message));
        }

        public void WriteLine(string message, LogInfo info)
        {
            WriteLine(message,info, LogLevel.Minimal);
        }

        public void WriteLine(Exception ex, LogInfo info, LogLevel level)
        {
            if ( _logLevel >= level && NotifyEvent != null)
                NotifyEvent(info, PerformMessage(ex));
        }

        public void WriteLine(Exception ex, LogInfo info)
        {
            WriteLine(ex,info, LogLevel.Minimal);
        }

        public void WriteLine(string message, Exception ex, LogInfo info, LogLevel level)
        {
            
        }
    }
}