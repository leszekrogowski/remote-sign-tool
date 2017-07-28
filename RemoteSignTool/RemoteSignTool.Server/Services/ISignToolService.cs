using RemoteSignTool.Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteSignTool.Server.Services
{
    public interface ISignToolService
    {
        bool TryToFindSignToolPath(out string path);

        Task<SignResultDto> Sign(string signToolPath, string signToolArguments, string workingDirectory);            
    }
}
