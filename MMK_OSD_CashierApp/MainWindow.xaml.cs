using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MMK_OSD_CashierApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // App DB.
        public DB db = new DB();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;

            if (hyperlink == null)
                return;

            try
            {
                Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = hyperlink.NavigateUri.ToString(),
                        UseShellExecute = true,
                    }
                    );
            }
            catch(Exception ex) { }
        }

        private void button_Login_CustomersClub_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = true
            };

            var workerVM = new ViewModels.Worker_ViewModel();

            worker.DoWork += (sender, e) =>
            {
                workerVM.ProgressState = "در حال بررسی اتصال به پایگاه داده";

                // Check connection with ROOT DB.
                var dbResult_TestRoot = db.sql_TestRootConnection().Result;
                if(dbResult_TestRoot.result != DB.DBResultEnum.DB_OK)
                {
                    Application.Current.Dispatcher.Invoke(() => workerVM.ProgressColor = new SolidColorBrush(Color.FromRgb(255, 255, 0)));
                    workerVM.ProgressState = "پایگاه داده با رمز Root محافظت شده است.";

                    Task.Delay(100).Wait();
                }

                // Wait a little...
                Task.Delay(500).Wait();
            };

            Dialog_Worker workerDialog = new(worker, workerVM);
            workerDialog.ShowDialog();
        }
    }
}