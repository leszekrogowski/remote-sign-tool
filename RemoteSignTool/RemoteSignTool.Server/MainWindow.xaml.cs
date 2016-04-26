using System.Windows;
using GalaSoft.MvvmLight;
using RemoteSignTool.Server.ViewModel;

namespace RemoteSignTool.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) =>
            {
                var viewModel = DataContext as ICleanup;
                if (viewModel != null)
                {
                    viewModel.Cleanup();
                }

                ViewModelLocator.Cleanup();
            };
        }
    }
}