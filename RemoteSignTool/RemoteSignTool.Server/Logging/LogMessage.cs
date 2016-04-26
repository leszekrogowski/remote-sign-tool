using NLog;

namespace RemoteSignTool.Server.Logging
{
    public class LogMessage
    {
        public LogMessage(string logLevel, string message)
        {
            LogLevel = logLevel;
            Message = message;
        }

        public string LogLevel { get; private set; }

        public string Message { get; private set; }
    }
}
