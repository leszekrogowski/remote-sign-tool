using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using NLog;
using RemoteSignTool.Common.Dto;

namespace RemoteSignTool.Server.Services
{
    public class SignToolService : ISignToolService
    {
        private const string SignToolX64Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe";
        private const string SignToolX86Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x86\signtool.exe";
        private const string WindowsSDKRootPath = @"C:\Program Files (x86)\Windows Kits\10\bin\";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                DirectoryInfo sdkRoot = new DirectoryInfo(WindowsSDKRootPath);
                DirectoryInfo[] subDirs = sdkRoot.GetDirectories("10.*", SearchOption.AllDirectories);

                foreach (DirectoryInfo dirInfo in subDirs.Reverse())
                {
                    string sdkPath = dirInfo.FullName;
                    Logger.Log(LogLevel.Info, $"Searching for signtool in {sdkPath}...");

                    string signToolPath = Path.Combine(sdkPath, "x64", "signtool.exe");

                    if (File.Exists(signToolPath))
                    {
                        path = signToolPath;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        public async Task<SignResultDto> Sign(string signToolPath, string signSubcommnands, string workingDirectory)
        {
            using (var process = new Process())
            {
                var signToolArguments = string.Format("sign {0} *.*", signSubcommnands);
                Logger.Info(Properties.Resources.ExecutingOnFormat, signToolPath, signToolArguments, workingDirectory);

                process.StartInfo.FileName = signToolPath;
                process.StartInfo.Arguments = signToolArguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.Start();

                string standardOutput = await process.StandardOutput.ReadToEndAsync();
                string standardError =  await process.StandardError.ReadToEndAsync();

                process.WaitForExit(300000);

                return new SignResultDto()
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = standardError,
                    StandardError = standardError
                };
            }
        }
    }
}