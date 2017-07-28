using RemoteSignTool.Common.Dto;
using RemoteSignTool.Server.Services;
using System;
using System.Threading.Tasks;

namespace RemoteSignTool.Server.Design
{
    public class DesignSignToolService : ISignToolService
    {
        public bool TryToFindSignToolPath(out string path)
        {
            path = @"C:\SamplePath\signtool.exe";
            return true;
        }

        public Task<SignResultDto> Sign(string signToolPath, string signToolArguments, string workingDirectory)
        {
            return Task.FromResult(new SignResultDto()
            {
                ExitCode = 0,
                StandardOutput = "",
                StandardError = ""
            });
        }
    }
}