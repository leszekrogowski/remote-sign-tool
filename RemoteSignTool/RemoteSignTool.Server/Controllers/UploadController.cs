using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using NLog;
using RemoteSignTool.Server.Results;
using RemoteSignTool.Server.ServerFiles;

namespace RemoteSignTool.Server.Controllers
{
    [RoutePrefix("api/upload")]
    public class UploadController : ApiController
    {
        #region Fields

        public const string UploadDirectoryName = "Upload";
        public const string DownloadRouteName = "Upload.Download";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        #endregion

        #region EventHandlers

        #endregion

        #region Public

        [Route("download/{fileName}", Name = DownloadRouteName)]
        [HttpGet]
        public IHttpActionResult Download(string fileName)
        {
            var serverFilePath = Path.Combine(UploadDirectoryName, fileName);
            if (!File.Exists(serverFilePath))
            {
                Logger.Warn("File not found for download: {0}");
                return this.NotFound();
            }

            Logger.Info("Downloading file: {0}", fileName);
            var mime = MimeMapping.GetMimeMapping(serverFilePath);
            return new FileResult(serverFilePath, mime);
        }

        [Route("save")]
        [HttpPost]
        public async Task<IHttpActionResult> Save()
        {
            // The implementation is based on article from: http://www.asp.net/web-api/overview/advanced/sending-html-form-data-part-2

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                Logger.Warn("Content not support for file upload");
                return this.StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            var rootPath = UploadDirectoryName;
            var provider = new CustomMultipartFormDataStreamProvider(rootPath);

            // Read the form data.
            await Request.Content.ReadAsMultipartAsync(provider);
            if (provider.FileData.Any())
            {
                Logger.Info("File(s): {0} sucessfully saved", string.Join(", ", provider.FileData.Select(fd => fd.LocalFileName)));
                return Ok();
            }
            else
            {
                Logger.Warn(Properties.Resources.NoFileSentErrorMessage);
                return BadRequest(Properties.Resources.NoFileSentErrorMessage);
            }
        }

        [Route("remove")]
        [HttpPost]
        public IHttpActionResult Remove(IEnumerable<string> fileNames)
        {
            if (fileNames != null)
            {
                foreach (var fileName in fileNames)
                {
                    Logger.Info("Removing file: {0}", fileName);
                    File.Delete(Path.Combine(UploadDirectoryName, fileName));
                }
            }

            return this.Ok();
        }

        #endregion

        #region Private

        #endregion
    }
}
