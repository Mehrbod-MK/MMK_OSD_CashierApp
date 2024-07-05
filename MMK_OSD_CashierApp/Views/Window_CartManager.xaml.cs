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

namespace MMK_OSD_CashierApp.Views
{
    /// <summary>
    /// Interaction logic for Window_CartManager.xaml
    /// </summary>
    public partial class Window_CartManager : Window
    {
        private Task? task_UpdateProduct = null;
        private CartManager_ViewModel? vm_Dashboard = null;

        public Window_CartManager(CartManager_ViewModel vm_Dashboard)
        {
            InitializeComponent();

            // Set binding Data Context.
            this.vm_Dashboard = vm_Dashboard;
            DataContext = vm_Dashboard;
        }

        private void TextBox_Enter_ProductCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (vm_Dashboard == null)
                return;

            if (task_UpdateProduct?.Status == TaskStatus.Running)
            {
                task_UpdateProduct.Dispose();
                task_UpdateProduct = null;
            }

            task_UpdateProduct = new Task(async () => await vm_Dashboard.FindProduct_AfterTime
                (Convert.ToUInt32(TextBox_Enter_ProductCode), TimeSpan.FromSeconds(3)));

            task_UpdateProduct.Start();
        }
    }
}
