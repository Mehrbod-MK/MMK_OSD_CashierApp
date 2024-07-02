using MMK_OSD_CashierApp.ViewModels;
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

            worker.RunWorkerCompleted += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() => this.IsEnabled = true);
            };

            worker.DoWork += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() => this.IsEnabled = false);

                workerVM.ProgressState = "در حال بررسی اتصال به پایگاه داده";

                // Check connection with ROOT DB.
                var dbResult_TestRoot = db.sql_TestRootConnection().Result;
                if(dbResult_TestRoot.result != DB.DBResultEnum.DB_OK)
                {
                    Application.Current.Dispatcher.Invoke(() => workerVM.ProgressColor = new SolidColorBrush(Color.FromRgb(255, 255, 0)));
                    workerVM.ProgressState = "پایگاه داده با رمز Root محافظت شده است.";

                    Task.Delay(100).Wait();

                    // Display password dialog box.
                    while (true)
                    {
                        MessageBubble_Result? msgBubble_Result = null;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var msgBubble_AskRootDBPassword =
                                new MessageBubble_View(
                                    "رمز پایگاه داده",
                                    "لطفاً رمز پایگاه داده MySQL را وارد کنید:",
                                    null,
                                    MessageBoxImage.Warning,
                                    msgBubble_Buttons: MessageBoxButton.OKCancel,
                                    isResponseAPassword: true
                                    );

                            msgBubble_Result = msgBubble_AskRootDBPassword.DisplayMessageBubble();
                        });

                        if (msgBubble_Result == null)
                            continue;

                        if (msgBubble_Result.dialogResult == "OK")
                        {
                            workerVM.ProgressState = "بررسی رمز...";

                            // Test the connection again.
                            if(msgBubble_Result.password != null)
                                dbResult_TestRoot = db.sql_TestRootConnection(DB.SecureStringToString(msgBubble_Result.password)).Result;

                            // If incorrect again, continue loop.
                            if (dbResult_TestRoot.result == DB.DBResultEnum.DB_ERROR)
                            {
                                continue;
                            }

                            // If password was OK, exit.
                            break;
                        }
                        else
                        {
                            // If Cancel or anything was pressed, exit loop.
                            return;
                        }
                    }
                }

                // Wait a little...
                Task.Delay(500).Wait();

                // Hide this window.
                Application.Current.Dispatcher.Invoke(() => this.Hide());

                // Finish worker -> Display Personnel Login Page.
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    var wnd_LoginPersonnel = new Login_Personnel(this);
                    wnd_LoginPersonnel.Show();
                };
            };

            Dialog_Worker workerDialog = new(worker, workerVM);
            workerDialog.ShowDialog();
        }
    }
}