using MMK_OSD_CashierApp.Models;
using MMK_OSD_CashierApp.ViewModels;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MMK_OSD_CashierApp
{
    /// <summary>
    /// Interaction logic for Login_Personnel.xaml
    /// </summary>
    public partial class Login_Personnel : Window
    {
        MainWindow ref_MainWindow;

        bool loginSuccessful = false;

        public Login_Personnel(MainWindow ptr_MainWindow)
        {
            InitializeComponent();

            ref_MainWindow = ptr_MainWindow;
        }

        private void Window_Login_Personnel_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!loginSuccessful)
            {
                if (
                    MessageBox.Show("آیا مایل به لغو ورود به برنامه هستید؟",
                    "هشدار",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning,
                    MessageBoxResult.No,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                    ) == MessageBoxResult.Yes)
                {
                    e.Cancel = false;
                    ref_MainWindow.Show();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = false;
                ref_MainWindow.Show();
            }
        }

        private void Button_Login_Personnel_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker_LoginPersonnel = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };

            Worker_ViewModel wvm_LoginPersonnel = new Worker_ViewModel();

            // Login logic.
            worker_LoginPersonnel.DoWork += (sender, e) =>
            {
                // Get username and convert password to SHA-1 hashed string.
                wvm_LoginPersonnel.ProgressState = "در حال رمزنگاری...";
                string username = "", password = "";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    username = TextBox_Personnel_Username.Text;
                    password = DB.Hash(PasswordBox_Personnel_Username.Password);
                });

                // Check if username and password match a record in database.
                wvm_LoginPersonnel.ProgressState = "در حال جستجوی پایگاه داده...";
                User? foundUser = DB._THROW_DBRESULT<User?>(MainWindow.db.db_Get_User(username).Result);
                if(foundUser == null || foundUser.LoginPassword.ToLower() != password.ToLower())
                {
                    // If no user found or password was wrong, set error state.
                    e.Result = DB.DB_QUERY_ERROR_USER_BAD_CREDENTIALS;
                    return;
                }

                // Check user role for personnel.
                if(foundUser.RoleFlags == (uint)DB.DB_Roles.DB_ROLE_Customer)
                {
                    // If user didn't have a personnel's role, restrict access.
                    e.Result = DB.DB_QUERY_ERROR_RESTRICTED_ACCESS;
                    return;
                }
                /*var db_GetUserQuery = MainWindow.db.sql_Execute_Query(
                    $"SELECT * FROM {DB.DB_TABLE_NAME_USERS} WHERE NationalID = \'{username}\' AND LoginPassword = \'{password}\'"
                    ).Result;
                
                MySqlDataReader? dataReader_Personnel = DB._THROW_DBRESULT
                <MySqlDataReader?>(db_GetUserQuery);
                if(dataReader_Personnel != null && !dataReader_Personnel.HasRows)
                {
                    // If no user found, set error state.
                    MainWindow.db.sql_End_Query(dataReader_Personnel).Wait();
                    e.Result = DB.DB_QUERY_ERROR_USER_BAD_CREDENTIALS;

                    return;
                }*/

                // Login was successful, go to personnel's profile.
                loginSuccessful = true;
                e.Result = DB.DB_QUERY_USER_OK;
            };

            worker_LoginPersonnel.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Result == null)
                    throw new NullReferenceException();

                if(e.Error != null)
                {
                    MessageBox.Show(
                    $"خطای ذیل به وقوع پیوست:\n\n{e.Error}",
                    "خطای سیستم",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                    );

                    return;
                }

                // Username/Password was incorrect.
                if((string)e.Result == DB.DB_QUERY_ERROR_USER_BAD_CREDENTIALS)
                {
                    MessageBox.Show(
                    $"نام کاربری یا کلمه عبور اشتباه است.",
                    "خطای ورود به سامانه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                    );

                    return;
                }

                // Restricted Access.
                if ((string)e.Result == DB.DB_QUERY_ERROR_RESTRICTED_ACCESS)
                {
                    MessageBox.Show(
                    $"شما مجاز به ورود به سامانه پرسنلی نمی‌باشید.",
                    "خطای ورود به سامانه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                    );

                    return;
                }

                // Successful Login.
                else if((string)e.Result == DB.DB_QUERY_USER_OK)
                {
                    this.Hide();

                    Views.Window_Dashboard dashboard = new(new Dashboard_ViewModel());
                    dashboard.Show();
                }
            };

            Dialog_Worker worker = new Dialog_Worker(worker_LoginPersonnel, wvm_LoginPersonnel);
            worker.ShowDialog();
        }

        private void TextBox_Personnel_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Button_Login_Personnel_Click(sender, new RoutedEventArgs());
            }
        }

        private void Window_Login_Personnel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
    }
}
