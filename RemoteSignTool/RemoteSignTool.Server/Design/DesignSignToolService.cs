using RemoteSignTool.Server.Services;

namespace RemoteSignTool.Server.Design
{
    public class DesignSignToolService : ISignToolService
    {
        public bool TryToFindSignToolPath(out string path)
        {
            path = @"C:\SamplePath\signtool.exe";
            return true;
        }
    }
}