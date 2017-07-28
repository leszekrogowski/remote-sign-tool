using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Ionic.Zip;
using NLog;
using RemoteSignTool.Common.Dto;
using RemoteSignTool.Server.Services;

namespace RemoteSignTool.Server.Controllers
{
    [RoutePrefix("api/signtool")]
    public class SignToolController : ApiController
    {
        private const string TempDirectoryName = "Temp";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISignToolService _signToolService;

        public SignToolController(ISignToolService signToolService)
        {
            _signToolService = signToolService;
        }

        [HttpGet]
        public IHttpActionResult Ping()
        {
            Logger.Info("Ping received");
            return this.Ok();
        }

        [Route("sign")]
        [HttpPost]
        public async Task<IHttpActionResult> Sign([FromBody]SignDto dto)
        {
            Logger.Info(Properties.Resources.StartSigningFiles);

            var archivePath = Path.Combine(UploadController.UploadDirectoryName, dto.ArchiveName);
            if (!File.Exists(archivePath))
            {
                Logger.Warn(Properties.Resources.ArchiveHasNotBeenFoundFormat, dto.ArchiveName);
                return this.BadRequest(string.Format(Properties.Resources.ArchiveHasNotBeenFoundFormat, dto.ArchiveName));
            }

            var extractionDirectoryName = Path.Combine(TempDirectoryName, Path.GetRandomFileName());
            Directory.CreateDirectory(extractionDirectoryName);
            Logger.Info(Properties.Resources.DirectoryCreatedFormat, extractionDirectoryName);

            using (ZipFile zip = new ZipFile(archivePath))
            {
                zip.ExtractAll(extractionDirectoryName);
                Logger.Info(Properties.Resources.FilesHaveBeenExtractedFormat, extractionDirectoryName);
            }

            string signToolPath;
            if (!_signToolService.TryToFindSignToolPath(out signToolPath))
            {
                Logger.Error(Properties.Resources.SignToolNotInstalled);
                return this.InternalServerError(new FileNotFoundException(Properties.Resources.SignToolNotInstalled, "signtool.exe"));
            }

            var signResult = await _signToolService.Sign(signToolPath, dto.SignSubcommands, extractionDirectoryName);
                      
            var signedArchiveName = string.Format("{0}_signed.zip", Path.GetFileNameWithoutExtension(dto.ArchiveName));
            if (signResult.ExitCode == 0)
            {
                Logger.Info(Properties.Resources.SignToolSuccessfullySignedFiles);
                using (ZipFile zip = new ZipFile())
                {
                    // Currently, we flatten the hierarchy of files for sake of simplicity
                    zip.AddFiles(Directory.GetFiles(extractionDirectoryName), false, string.Empty);
                    zip.Save(Path.Combine(UploadController.UploadDirectoryName, signedArchiveName));
                    Logger.Info(Properties.Resources.ArchiveWithSignedFilesCreated, signedArchiveName);
                }
            }
            else
            {
                Logger.Error(Properties.Resources.SignToolExitedWithCodeFormat, signResult.ExitCode);
                Logger.Error(signResult.StandardError);
            }

            var result = new SignResultDto()
            {
                ExitCode = signResult.ExitCode,
                StandardOutput = signResult.StandardOutput,
                StandardError = signResult.StandardError,
                DownloadUrl = signResult.ExitCode == 0 ? this.Url.Link(UploadController.DownloadRouteName, new { fileName = Uri.EscapeUriString(signedArchiveName) }) : null
            };

            return this.Ok(result);
        }
    }
}
