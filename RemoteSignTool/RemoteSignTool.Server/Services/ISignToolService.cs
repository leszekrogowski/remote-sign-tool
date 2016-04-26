using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteSignTool.Server.Services
{
    public interface ISignToolService
    {
        bool TryToFindSignToolPath(out string path);
    }
}
