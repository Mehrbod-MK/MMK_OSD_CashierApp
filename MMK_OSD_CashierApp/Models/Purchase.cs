using MMK_OSD_CashierApp.ViewModels;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMK_OSD_CashierApp.Models
{
    public class Purchase : ViewModelBase
    {
        private uint purchaseID;
        private string customer_NationalID = string.Empty;
        private DateTime dateTimeSubmitted;
        private string submittedBy_NationalID = string.Empty;
        private uint num_ProductsPurchased;
        private ulong total_Price;
        private ulong total_Discount;
        private ulong total_Payment;

        public uint PurchaseID { get => purchaseID; set => SetProperty(ref purchaseID, value); }
        public string Customer_NationalID { get => customer_NationalID; set => SetProperty(ref customer_NationalID, value); }
        public DateTime DateTimeSubmitted { get => dateTimeSubmitted; set => SetProperty(ref dateTimeSubmitted, value); }
        public string SubmittedBy_NationalID { get => submittedBy_NationalID; set => SetProperty(ref submittedBy_NationalID, value); }
        public uint Num_ProductsPurchased { get => num_ProductsPurchased; set => SetProperty(ref num_ProductsPurchased, value); }
        public ulong Total_Price { get => total_Price; set => SetProperty(ref total_Price, value); }
        public ulong Total_Discount { get => total_Price; set => SetProperty(ref total_Discount, value); }
        public ulong Total_Payment { get => total_Price; set => SetProperty(ref total_Payment, value); }
    }
}
