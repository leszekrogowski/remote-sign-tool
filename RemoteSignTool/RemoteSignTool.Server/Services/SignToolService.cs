using System.IO;

namespace RemoteSignTool.Server.Services
{
    public class SignToolService : ISignToolService
    {
        private const string SignToolX64Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe";
        private const string SignToolX86Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x86\signtool.exe";

        public bool TryToFindSignToolPath(out string path)
        {
            if (File.Exists(SignToolX64Path))
            {
                path = SignToolX64Path;
                return true;
            }
            else if (File.Exists(SignToolX86Path))
            {
                path = SignToolX86Path;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }
    }
}