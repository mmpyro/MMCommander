using LogLib;

namespace Comander.Core
{
    public class LogMsg
    {
        public LogInfo Info { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }

        public LogMsg(LogInfo info, string message)
        {
            Info = info;
            // "time|message"; the message itself may contain '|' (BUG-27).
            string[] param = message.Split('|', 2);
            Time = param.Length == 2 ? param[0] : string.Empty;
            Message = param[param.Length - 1];
        }

        public override string ToString()
        {
            return Message;
        }
    }
}