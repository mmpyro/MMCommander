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
            string[] param = message.Split('|');
            Time = param[0];
            Message = param[1];
        }

        public override string ToString()
        {
            return Message;
        }
    }
}