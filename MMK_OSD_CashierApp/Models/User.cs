using MMK_OSD_CashierApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMK_OSD_CashierApp.Models
{
    public class User : ViewModelBase
    {
        #region User_Private_Fields

        private string nationalID = string.Empty;
        private string password = string.Empty;
        private string? optionalUsername;
        private string? firstName;
        private string? lastName = null;
        private uint roleFlags;
        private string? email;
        private DateTime registerDateTime;
        private DateTime? lastLoginDateTime;

        #endregion

        #region User_Properties

        public string NationalID
        {
            get => nationalID;
            set => SetProperty(ref nationalID, value);
        }
        public string LoginPassword
        {
            get => password;
            set => SetProperty(ref password, value);
        }
        public string? OptionalUserName
        {
            get => optionalUsername;
            set => SetProperty(ref optionalUsername, value);
        }
        public string? FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value);
        }
        public string? LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value);
        }
        public uint RoleFlags
        {
            get => roleFlags;
            set => SetProperty(ref roleFlags, value);
        }
        public string? Email
        {
            get => email;
            set => SetProperty(ref email, value);
        }
        public DateTime RegisterDateTime
        {
            get => registerDateTime;
            set => SetProperty(ref registerDateTime, value);
        }
        public DateTime? LastLoginDateTime
        {
            get => lastLoginDateTime;
            set => SetProperty(ref lastLoginDateTime, value);
        }

        #endregion
    }
}
