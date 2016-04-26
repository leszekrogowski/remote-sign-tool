namespace RemoteSignTool.Client
{
    public static class ErrorCodes
    {
        public const int Ok = 0;
        public const int NoArguments = 1;
        public const int UnsupportedCommand = 2;
        public const int UnsupportedSubcommand = 3;
        public const int UnknownSubcommand = 4;
        public const int SignToolInvalidExitCode = 5;
        public const int MultipleFilesWithTheSameNameNotSupported = 6;
    }
}
