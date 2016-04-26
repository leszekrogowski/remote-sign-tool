using GalaSoft.MvvmLight.Messaging;
using NLog;

namespace RemoteSignTool.Server.Logging
{
    // The class is used by NLog config that is why there are 0 references in here
    public static class LoggingHelper
    {
        public static void NotifyAboutLogEvent(string logLevel, string message)
        {
            Messenger.Default.Send(new LogMessage(logLevel, message));
        }
    }
}
