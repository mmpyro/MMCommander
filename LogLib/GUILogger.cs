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

        public void Info(Exception ex)
        {
            PerformInfoMessage(ex.ToString());
        }

        public void Warn(Exception ex)
        {
            PerformWarnMessage(ex.ToString());
        }

        public void Error(Exception ex)
        {
            PerformErrorMessage(ex.ToString());
        }

        public void Info(string message, Exception ex)
        {
            PerformInfoMessage(message+" | "+ex);
        }

        public void Warn(string message, Exception ex)
        {
            PerformWarnMessage(message+" | "+ex);
        }

        public void Error(string message, Exception ex)
        {
            PerformErrorMessage(message+" | "+ex);
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

        protected string PerformLogMessage(Exception ex)
        {
            return PerformLogMessage(ex.Message);
        }


    }
}