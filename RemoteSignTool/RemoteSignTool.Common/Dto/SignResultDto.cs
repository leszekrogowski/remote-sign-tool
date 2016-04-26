namespace RemoteSignTool.Common.Dto
{
    public class SignResultDto
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public string DownloadUrl { get; set; }
    }
}
