using GalaSoft.MvvmLight.Messaging;

namespace RemoteSignTool.Server.Logging
{
    // The class is used by NLog config that is why there are 0 references in here
    public static class LoggingHelper
    {
        public static void NotifyAboutLogEvent(string logLevel, string message, string exceptionMessage)
        {
            var messageToSend = !string.IsNullOrWhiteSpace(exceptionMessage) ? string.Format("{0} {1}", message, exceptionMessage) : message;
            Messenger.Default.Send(new LogMessage(logLevel, messageToSend));
        }
    }
}
