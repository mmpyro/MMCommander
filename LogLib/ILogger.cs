using System;

namespace LogLib
{
    public enum LogLevel
    {
        Minimal,
        Debug
    }

    public enum LogInfo
    {
        Info,
        Warrning,
        Error,
    }

    public interface ILogger
    {
        void WriteLine(string message, LogInfo info, LogLevel level);
        void WriteLine(string message, LogInfo info);
        void WriteLine(Exception ex, LogInfo info, LogLevel level);
        void WriteLine(Exception ex, LogInfo info);
        void WriteLine(string message, Exception ex,LogInfo info , LogLevel level);
    }
}