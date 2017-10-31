using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using Ionic.Zip;
using NLog;
using RemoteSignTool.Common.Dto;

namespace RemoteSignTool.Client
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string TempDirectoryName = "Temp";
        private static readonly Dictionary<string, int> supportedSignOptions = new Dictionary<string, int>
        {
            // Certificate selection options
            { "/a", 0 },
            { "/c", 1 },
            { "/i", 1 },
            { "/n", 1 },
            { "/r", 1 },
            { "/s", 1 },
            { "/sm", 0 },
            { "/sha1", 1 },
            { "/fd", 1 },
            { "/u", 1 },
            { "/uw", 0 },
            // Private Key selection options
            { "/csp", 1 },
            { "/kc", 1 },
            // Signing parameter options
            { "/as", 0 },
            { "/d", 1 },
            { "/du", 1 },
            { "/t", 1 },
            { "/tr", 1 },
            { "/tseal", 1 },
            { "/td", 1 },
            // I'm not sure if I correctly interpret: "This option may be given multiple times.
            { "/sa", 2 },
            { "/seal", 0 },
            { "/itos", 0 },
            { "/force", 0 },
            { "/nosealwarn", 0 },
            // Other options
            { "/ph", 0 },
            { "/nph", 0 },
            { "/rmc", 0 },
            { "/q", 0 },
            { "/v", 0 },
            { "/debug", 0 }
        };

        private static readonly Dictionary<string, int> unsupportedSignOptions = new Dictionary<string, int>()
        {
            // Certificate selection options
            { "/ac", 1 },
            { "/f", 1 },
            { "/p", 1 },
            // Digest options
            { "/dg", 1 },
            { "/ds", 0 },
            { "/di", 1 },
            { "/dxml", 0 },
            { "/dlib", 1 },
            { "/dmdf", 1 },
            // PKCS7 options
            { "/p7", 1 },
            { "/p7co", 1 },
            { "/p7ce", 1 }
        };

        static int Main(string[] args)
        {
            if (!args.Any())
            {
                Logger.Error("Invalid number of arguments");
                return ErrorCodes.NoArguments;
            }

            if (args[0] != "sign")
            {
                Logger.Error("Remote signtool supports only sign command");
                return ErrorCodes.UnsupportedCommand;
            }

            var serverBaseAddress = ConfigurationManager.AppSettings["ServerBaseUrl"];
            if (string.IsNullOrWhiteSpace(serverBaseAddress))
            {
                Logger.Error("ServerBaseUrl is not configured in App.config");
                return 2;
            }

            var filesToSign = new List<string>();
            var fileNameToDirectoryLookup = new Dictionary<string, string>();
            var signSubcommands = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (unsupportedSignOptions.ContainsKey(args[i]))
                {
                    Logger.Error("Subcommand: {0} is not supported", args[i]);
                    return ErrorCodes.UnsupportedSubcommand;
                }

                if (supportedSignOptions.ContainsKey(args[i]))
                {
                    for (int j = i; j <= i + supportedSignOptions[args[i]]; j++)
                    {
                        // Save supported sign subcommands for furture use
                        signSubcommands.Add(args[j].Any(char.IsWhiteSpace) ? string.Format("\"{0}\"", args[j]) : args[j]);
                    }

                    i += supportedSignOptions[args[i]];
                    continue;
                }

                if (args[i].StartsWith(@"/"))
                {
                    Logger.Error("Unknown subcommand: {0}", args[i]);
                    return ErrorCodes.UnknownSubcommand;
                }

                var directoryName = Path.GetDirectoryName(args[i]);
                if (string.IsNullOrEmpty(directoryName))
                {
                    directoryName = ".";
                }

                // Path.GetFileName(args[i]) - it doesn't have to be exact file name, e.g. *.msi
                var matchingFiles = Directory.GetFiles(directoryName, Path.GetFileName(args[i]));

                foreach (var matchingFilePath in matchingFiles)
                {
                    // This is exact file name
                    var matchingFileName = Path.GetFileName(matchingFilePath);
                    if (!fileNameToDirectoryLookup.ContainsKey(matchingFileName))
                    {
                        filesToSign.Add(matchingFilePath);
                        fileNameToDirectoryLookup.Add(matchingFileName, directoryName);
                    }
                    else
                    {
                        Logger.Error("Current version doesn't support multiple files with the same name for single signature.");
                        Logger.Error("File names: {0}", matchingFilePath);
                        return ErrorCodes.MultipleFilesWithTheSameNameNotSupported;
                    }
                }
            }

            Directory.CreateDirectory(TempDirectoryName);
            var archiveToUploadName = string.Format("{0}.zip", Path.GetRandomFileName());
            var archiveToUploadPath = Path.Combine(TempDirectoryName, archiveToUploadName);
            using (ZipFile zip = new ZipFile())
            {
                // Currently, we flatten the hierarchy of files for sake of simplicity
                zip.AddFiles(filesToSign, false, string.Empty);
                zip.Save(archiveToUploadPath);
            }

            string signedArchivePath;
            try
            {
                Logger.Info($"Uploading zip file: {archiveToUploadName}, containing files: {string.Join(", ", filesToSign.ToArray())}");
                signedArchivePath = CommunicateWithServer(archiveToUploadPath, string.Join(" ", signSubcommands)).Result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to communicate with server");
                return ErrorCodes.ServerCommunicationFailed;
            }
            finally
            {
                File.Delete(archiveToUploadPath);
            }

            if (!string.IsNullOrEmpty(signedArchivePath))
            {
                var targetDirectoryName = signedArchivePath.Substring(0, signedArchivePath.Length - 4);
                using (var zip = new ZipFile(signedArchivePath))
                {
                    zip.ExtractAll(targetDirectoryName);
                }

                foreach (var signedFile in Directory.GetFiles(targetDirectoryName))
                {
                    var signedFileNameWithoutDir = Path.GetFileName(signedFile);
                    File.Copy(signedFile, Path.Combine(fileNameToDirectoryLookup[signedFileNameWithoutDir], signedFileNameWithoutDir), true);
                }

                Directory.Delete(targetDirectoryName, true);
                File.Delete(signedArchivePath);
                return ErrorCodes.Ok;
            }
            else
            {
                return ErrorCodes.SignToolInvalidExitCode;
            }
        }

        private static async Task<string> CommunicateWithServer(string archivePath, string signSubcommands)
        {
            var progressHandler = new ProgressMessageHandler();

            progressHandler.HttpSendProgress += SendProgressHandler;
            progressHandler.HttpReceiveProgress += ReceiveProgressHandler;


            using (var client = HttpClientFactory.Create(progressHandler))
            {
                var archiveName = Path.GetFileName(archivePath);
                var serverBaseAddress = ConfigurationManager.AppSettings["ServerBaseUrl"];
                client.BaseAddress = new Uri(serverBaseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(File.OpenRead(archivePath));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = Path.GetFileName(archivePath)
                    };
                    content.Add(fileContent);

                    var requestUri = "api/upload/save";
                    var uploadResponse = await client.PostAsync(requestUri, content);

                    if (!uploadResponse.IsSuccessStatusCode)
                    {
                        await ShowErrorAsync(uploadResponse);
                        return null;
                    }
                }

                var signRequestDto = new SignDto()
                {
                    ArchiveName = archiveName,
                    SignSubcommands = signSubcommands
                };

                Logger.Info($"Perform sign for: {archiveName}, using commands: {signSubcommands}");

                var signResponse = await client.PostAsJsonAsync("api/signtool/sign", signRequestDto);
                if (!signResponse.IsSuccessStatusCode)
                {
                    await ShowErrorAsync(signResponse);
                    return null;
                }

                var signResponseDto = await signResponse.Content.ReadAsAsync<SignResultDto>();
                if (signResponseDto.ExitCode != 0)
                {
                    Logger.Error("signtool.exe exited with code: {0}", signResponseDto.ExitCode);
                    Logger.Error(signResponseDto.StandardOutput);
                    Logger.Error(signResponseDto.StandardError);
                    return null;
                }

                Logger.Info($"Begin to download signed archive: {archiveName}");

                var downloadReponse = await client.GetStreamAsync(signResponseDto.DownloadUrl);
                var signedArchiveName = Path.GetFileName(signResponseDto.DownloadUrl);
                var signedArchivePath = Path.Combine(TempDirectoryName, signedArchiveName);

                using (var fileStream = File.Create(signedArchivePath))
                {
                    await downloadReponse.CopyToAsync(fileStream);
                }

                Logger.Info($"Delete archives {archiveName}, {signedArchiveName} from server");

                await client.PostAsJsonAsync("api/upload/remove", new List<string>() { archiveName, signedArchiveName });
                return signedArchivePath;
            }
        }

        private static async Task ShowErrorAsync(HttpResponseMessage response)
        {
            Logger.Error("Status code: {0}", response.StatusCode);
            Logger.Error(await response.Content.ReadAsStringAsync());
        }
        private static void SendProgressHandler(object sender, HttpProgressEventArgs e)
        {
            LogProgress("Sending:", e);
        }

        private static void ReceiveProgressHandler(object sender, HttpProgressEventArgs e)
        {
            LogProgress("Receive:", e);
        }

        private static void LogProgress(string prefix, HttpProgressEventArgs e)
        {
            Logger.Info($"{prefix} ({e.ProgressPercentage}%) Transfered: {e.BytesTransferred}, Total: {e.TotalBytes}");
        }
    }
}
