using MMK_OSD_CashierApp.Models;
using MMK_OSD_CashierApp.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private User? user_LoggedIn = null;
        public User? User_LoggedIn
        {
            get => user_LoggedIn;
            private set => SetProperty(ref user_LoggedIn, value);
        }

        public Dashboard_ViewModel(User user_LoggedIn)
        {
            this.user_LoggedIn = user_LoggedIn;

            cmd_CreateNewCart = new RelayCommand(Order_CreateNewCart, Allow_CreateNewCart);
        }

        public void Order_CreateNewCart(object? parameter)
        {
            (parameter as Window)?.Hide();
            if (User_LoggedIn == null)
                return;

            Window_CartManager wnd_CartManager = new Window_CartManager(new CartManager_ViewModel(user_LoggedIn.NationalID));
            wnd_CartManager.ShowDialog();
        }
        public bool Allow_CreateNewCart(object? parameter)
        {
   
            return (user_LoggedIn != null) && 
                ((user_LoggedIn.RoleFlags | (uint)DB.DB_Roles.DB_ROLE_Cashier) != 0);
        }

        #endregion
    }
}
