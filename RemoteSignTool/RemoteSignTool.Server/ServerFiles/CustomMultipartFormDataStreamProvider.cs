using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RemoteSignTool.Server.ServerFiles
{
    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public CustomMultipartFormDataStreamProvider(string rootPath)
            : base(rootPath)
        {
        }

        public CustomMultipartFormDataStreamProvider(string rootPath, int bufferSize)
            : base(rootPath, bufferSize)
        {
        }

        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            return !string.IsNullOrEmpty(headers.ContentDisposition.FileName)
                ? headers.ContentDisposition.FileName.Replace("\"", string.Empty)
                : base.GetLocalFileName(headers);
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            Directory.CreateDirectory(this.RootPath);
            return base.GetStream(parent, headers);
        }
    }
}
