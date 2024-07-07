using MMK_OSD_CashierApp.Models;
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
        // private Task? task_UpdateProduct = null;
        private CartManager_ViewModel vm_Dashboard;

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

            Product? foundProduct = null;

            if (!uint.TryParse(TextBox_Enter_ProductCode.Text, out uint productID))
            {
                vm_Dashboard.FoundProduct = null;
                return;
            }

            Task.Run(async () =>
            {
                foundProduct = (Product?)DB._THROW_DBRESULT
                <Product?>(await MainWindow.db.db_Get_Product(productID));
            }).ContinueWith((x) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    vm_Dashboard.FoundProduct = foundProduct;
                });
            });
        }

        private void ListView_Cart_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var command_RemoveAll = vm_Dashboard.Command_RemoveAllCart as RelayCommand;
            if (command_RemoveAll == null)
                return;

            command_RemoveAll.InvokeCanExecuteChanged();
        }

        private void ListView_Cart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var command_RemoveAll = vm_Dashboard.Command_RemoveAllCart as RelayCommand;
            if (command_RemoveAll == null)
                return;

            command_RemoveAll.InvokeCanExecuteChanged();
        }

        private void TextBox_Enter_CustomerNationalID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.DataContext is not CartManager_ViewModel vm_CartMgr)
                return;
            if (vm_CartMgr.Command_RegisterNewCustomer is not RelayCommand cmd)
                return;

            cmd.InvokeCanExecuteChanged();
        }
    }
}
