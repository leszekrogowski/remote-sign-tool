using System.Threading.Tasks;
using RemoteSignTool.Common.Dto;

namespace RemoteSignTool.Server.Services
{
    public interface ISignToolService
    {
        bool TryToFindSignToolPath(out string path);

        Task<SignResultDto> Sign(string signToolPath, string signToolArguments, string workingDirectory);            
    }
}
