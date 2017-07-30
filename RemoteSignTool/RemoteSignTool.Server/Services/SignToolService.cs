using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NLog;
using RemoteSignTool.Common.Dto;

namespace RemoteSignTool.Server.Services
{
    public class SignToolService : ISignToolService
    {
        private const string SignToolX64Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe";
        private const string SignToolX86Path = @"C:\Program Files (x86)\Windows Kits\10\bin\x86\signtool.exe";
        private const string SignTool_15063_X64Path = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe";

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
            else if (File.Exists(SignTool_15063_X64Path))
            {
                path = SignTool_15063_X64Path;
                return true;
            }
            else
            {
                path = null;
                return false;
            }
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