using System;

namespace LogLib
{
    public class GUILogger : ILogger
    {
        public void Info(string message)
        {
            PerformInfoMessage(message);
        }

        public void Warn(string message)
        {
            PerformWarnMessage(message);
        }

        public void Error(string message)
        {
            PerformErrorMessage(message);
        }

        public void Debug(string message)
        {
            
        }

        public void Info(Exception ex)
        {
            PerformInfoMessage(ex.Message);
        }

        public void Warn(Exception ex)
        {
            PerformWarnMessage(ex.Message);
        }

        public void Error(Exception ex)
        {
            PerformErrorMessage(ex.Message);
        }

        public void Info(string message, Exception ex)
        {
            PerformInfoMessage(WithReason(message, ex));
        }

        public void Warn(string message, Exception ex)
        {
            PerformWarnMessage(WithReason(message, ex));
        }

        public void Error(string message, Exception ex)
        {
            PerformErrorMessage(WithReason(message, ex));
        }

        public event Notify NotifyEvent;

        protected void PerformInfoMessage(string message)
        {
            if (NotifyEvent != null)
                NotifyEvent(LogInfo.Info, PerformLogMessage(message));
        }

        protected void PerformErrorMessage(string message)
        {
            if (NotifyEvent != null)
                NotifyEvent(LogInfo.Error, PerformLogMessage(message));
        }

        protected void PerformWarnMessage(string message)
        {
            if (NotifyEvent != null)
                NotifyEvent(LogInfo.Warrning, PerformLogMessage(message));
        }

        protected string PerformLogMessage(string message)
        {
            DateTime dt = DateTime.Now;
            return string.Format("{0}|{1}",dt.ToShortTimeString() , message);
        }

        // The GUI shows a short reason; the full stack trace goes to the log file (FileLogger).
        private static string WithReason(string message, Exception ex)
        {
            return string.IsNullOrEmpty(ex?.Message) ? message : message + ": " + ex.Message;
        }

        public void Fatal(Exception ex)
        {
            Error("Unexpected error: " + ex.Message + " (details in the log file)");
        }
    }
}