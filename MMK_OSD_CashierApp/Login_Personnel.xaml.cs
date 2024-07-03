using MMK_OSD_CashierApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }
    }
}
