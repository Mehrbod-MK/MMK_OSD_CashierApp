using MMK_OSD_CashierApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class Dashboard_ViewModel : ViewModelBase
    {
        #region Public_Commands

        private RelayCommand cmd_CreateNewCart;
        public ICommand Command_CreateNewCart => cmd_CreateNewCart;

        private string user_NationalID;
        public string User_NationalID
        {
            get => user_NationalID;
            private set => SetProperty(ref user_NationalID, value);
        }

        public Dashboard_ViewModel(string user_NationalID)
        {
            this.user_NationalID = user_NationalID;

            cmd_CreateNewCart = new RelayCommand(Order_CreateNewCart, Allow_CreateNewCart);
        }

        public void Order_CreateNewCart(object? parameter)
        {
            (parameter as Window)?.Hide();

            Window_CartManager wnd_CartManager = new Window_CartManager(new CartManager_ViewModel(User_NationalID));
            wnd_CartManager.ShowDialog();
        }
        public bool Allow_CreateNewCart(object? parameter)
        {
            return true;
        }

        #endregion
    }
}
