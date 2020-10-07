using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace RemoteSignTool.Server.Results
{
    public class FileResult : IHttpActionResult
    {
        private readonly string _filePath;
        private readonly string _contentType;
        private readonly string _dispositionType;
        private readonly string _dispositionName;

        public FileResult(
            string filePath,
            string contentType = null,
            string dispositionType = null,
            string dispositionName = null)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this._filePath = filePath;
            this._contentType = contentType;
            this._dispositionType = dispositionType;
            this._dispositionName = dispositionName;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                () =>
                {
                    const int streamBufferSize = 1024 * 1024; // 1MB buffer size
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(File.OpenRead(this._filePath), streamBufferSize)
                    };

                    var contentType = this._contentType ?? MimeMapping.GetMimeMapping(Path.GetFileName(this._filePath));
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    if (!string.IsNullOrEmpty(this._dispositionType))
                    {
                        response.Content.Headers.ContentDisposition.DispositionType = this._dispositionType;
                    }

                    if (!string.IsNullOrEmpty(this._dispositionName))
                    {
                        response.Content.Headers.ContentDisposition.Name = this._dispositionName;
                    }

                    return response;

                }, cancellationToken);
        }
    }
}
