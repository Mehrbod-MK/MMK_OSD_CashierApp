using Google.Protobuf.WellKnownTypes;
using MMK_OSD_CashierApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace MMK_OSD_CashierApp.Models
{
    public class Product : ViewModelBase
    {
        private uint productID;
        private string productName = string.Empty;
        private ulong price;
        private string? vendor = null;
        private DateTime dateTimeSubmitted;
        private uint quantity;

        public uint ProductID { get => productID; set => SetProperty(ref productID, value); }
        public string ProductName { get => productName; set => SetProperty(ref productName, value); }
        public ulong Price { get => price; set => SetProperty(ref price, value); }
        public string? Vendor { get => vendor; set => SetProperty(ref vendor, value); }
        public DateTime DateTimeSubmitted { get => dateTimeSubmitted; set => SetProperty(ref dateTimeSubmitted, value); }
        public uint Quantity { get => quantity; set => SetProperty(ref quantity, value); }

        public void ForceNotifyChanges()
        {
            SetProperty(ref productID, productID, "ProductID", forceUpdate: true);
            SetProperty(ref productName, productName, "ProductName", forceUpdate: true);
            SetProperty(ref price, price, "Price", forceUpdate: true);
            SetProperty(ref vendor, vendor, "Vendor", forceUpdate: true);
            SetProperty(ref dateTimeSubmitted, dateTimeSubmitted, "DateTimeSubmitted", forceUpdate: true);
            SetProperty(ref quantity, quantity, "Quantity", forceUpdate: true);
        }
    }
}
