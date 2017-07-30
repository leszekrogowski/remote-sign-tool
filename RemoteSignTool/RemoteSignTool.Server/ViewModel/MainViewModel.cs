using System;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Owin.Hosting;
using NLog;
using RemoteSignTool.Server.Logging;
using RemoteSignTool.Server.Services;

namespace RemoteSignTool.Server.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly RelayCommand _startServerCommand;
        private readonly RelayCommand _stopServerCommand;
        private readonly ISignToolService _signToolService;
        private readonly StringBuilder _logBuilder;
        private IDisposable _httpServer;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(ISignToolService signToolService)
        {
            _signToolService = signToolService;
            _logBuilder = new StringBuilder();

            _startServerCommand = new RelayCommand(StartServer, CanStartServer);
            _stopServerCommand = new RelayCommand(StopServer, CanStopServer);

            MessengerInstance.Register<LogMessage>(
                this,
                message =>
                {
                    _logBuilder.AppendLine(message.Message);
                    RaisePropertyChanged(() => Log);
                });

            this.BaseAddress = Properties.Settings.Default.BaseAddress;

            string signToolPath;
            if (!_signToolService.TryToFindSignToolPath(out signToolPath))
            {
                Logger.Error(Properties.Resources.SignToolNotInstalled);
            }
        }

        private string _serverStatus = Properties.Resources.Label_ServerIsNotRunning;

        public string ServerStatus
        {
            get
            {
                return _serverStatus;
            }

            set
            {
                Set(ref _serverStatus, value);
            }
        }

        private string _baseAddress;

        /// <summary>
        /// Sets and gets the BaseAddress property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string BaseAddress
        {
            get
            {
                return _baseAddress;
            }

            set
            {
                Set(ref _baseAddress, value);
            }
        }

        public string Log
        {
            get
            {
                // I'm not sure about efficiency of such approach
                return _logBuilder.ToString();
            }
        }

        public ICommand StartServerCommand
        {
            get
            {
                return _startServerCommand;
            }
        }

        public ICommand StopServerCommand
        {
            get
            {
                return _stopServerCommand;
            }
        }

        public override void Cleanup()
        {
            // Clean up if needed
            StopServer();

            Properties.Settings.Default.BaseAddress = this.BaseAddress;
            Properties.Settings.Default.Save();

            base.Cleanup();
        }

        private async void StartServer()
        {
            if (_httpServer != null)
            {
                return;
            }

            StartOptions options = new StartOptions();
            options.Urls.Add(this.BaseAddress);

            try
            {
                string signToolPath;
                if (!_signToolService.TryToFindSignToolPath(out signToolPath))
                {
                    Logger.Error(Properties.Resources.SignToolNotInstalled);
                    throw new Exception(Properties.Resources.SignToolNotInstalled);
                }

                var folderToSignAtStartup = Properties.Settings.Default.FolderToSignAtStartup;
                if (!string.IsNullOrWhiteSpace(folderToSignAtStartup))
                {
                    var signResult = await _signToolService.Sign(signToolPath, " /a", folderToSignAtStartup);
                    if (signResult.ExitCode != 0)
                    {
                        Logger.Error(Properties.Resources.SigningAtStartupFailed);
                        Logger.Error(Properties.Resources.SignToolExitedWithCodeFormat, signResult.ExitCode);
                        Logger.Error(signResult.StandardError);

                        throw new Exception(Properties.Resources.SigningAtStartupFailed);
                    }
                }

                _httpServer = WebApp.Start<Startup>(options);
                ServerStatus = Properties.Resources.Label_ServerIsRunning;
                _startServerCommand.RaiseCanExecuteChanged();
                _stopServerCommand.RaiseCanExecuteChanged();
                Logger.Info(Properties.Resources.ServerHasBeenStarted);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start server");
            }
        }

        private bool CanStartServer()
        {
            return _httpServer == null;
        }

        private void StopServer()
        {
            if (_httpServer != null)
            {
                _httpServer.Dispose();
                _httpServer = null;
                ServerStatus = Properties.Resources.Label_ServerIsNotRunning;
                _startServerCommand.RaiseCanExecuteChanged();
                _stopServerCommand.RaiseCanExecuteChanged();
                Logger.Info(Properties.Resources.ServerHasBeenStopped);
            }
        }

        private bool CanStopServer()
        {
            return _httpServer != null;
        }
    }
}