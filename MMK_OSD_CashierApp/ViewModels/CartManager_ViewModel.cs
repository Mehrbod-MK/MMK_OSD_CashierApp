using MMK_OSD_CashierApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class CartManager_ViewModel : ViewModelBase
    {
        /// <summary>
        /// Found product by its ID code.
        /// </summary>
        private Product? foundProduct;

        private ObservableCollection<Product> selectedProducts;

        public ObservableCollection<Product> SelectedProducts
        {
            get => selectedProducts;
            set => SetProperty(ref selectedProducts, value);
        }

        /// <summary>
        /// Found product, entered by its code.
        /// </summary>
        public Product? FoundProduct 
        { 
            get => foundProduct; 
            set 
            { 
                SetProperty(ref foundProduct, value); 
                command_AddToCart.InvokeCanExecuteChanged(); 
            } 
        }

        private ulong total_Price;
        private ulong total_Discount;
        private ulong total_Payment;
        public ulong TotalPrice
        {
            get => total_Price; 
            private set => SetProperty(ref total_Price, value);
        }
        public ulong TotalDiscount
        {
            get => total_Discount;
            private set => SetProperty(ref total_Discount, value);
        }
        public ulong TotalPayment
        {
            get => total_Payment;
            private set => SetProperty(ref total_Payment, value);
        }

        #region Public_CartManager_VM_Commands

        private RelayCommand command_AddToCart;
        public ICommand Command_AddToCart => command_AddToCart;
        public void Order_AddToCart(object? parameter)
        {
            if (FoundProduct == null)
                return;

            SelectedProducts.Add(FoundProduct);

            // Update 'Remove items' command.
            command_RemoveFromCart.InvokeCanExecuteChanged();
            command_RemoveAllCart.InvokeCanExecuteChanged();

            // Update Cash Values.
            Update_CashValues();

            TextBox? txtBox = parameter as TextBox;
            if (txtBox != null)
                txtBox.Focus();
        }
        public bool Allow_AddToCart(object? parameter)
        {
            return FoundProduct != null;
        }

        private RelayCommand command_RemoveFromCart;
        public ICommand Command_RemoveFromCart => command_RemoveFromCart;
        public void Order_RemoveFromCart(object? parameter)
        {
            if (parameter is not ListView listView_Cart)
                return;

            var selectedItems = listView_Cart.SelectedItems.Cast<Product>().ToList();

            foreach (var item in selectedItems)
            {
                SelectedProducts.Remove(item);
            }

            listView_Cart.SelectedIndex = -1;

            // Update Cash Values.
            Update_CashValues();

            // Update self and remove all.
            command_RemoveFromCart.InvokeCanExecuteChanged();
            command_RemoveAllCart.InvokeCanExecuteChanged();
        }
        public bool Allow_RemoveFromCart(object? parameter)
        {
            if (parameter is not ListView listView_Cart)
                return false;

            return SelectedProducts.Count > 0;
        }

        private RelayCommand command_RemoveAllCart;
        public ICommand Command_RemoveAllCart => command_RemoveAllCart;
        public void Order_RemoveAllCart(object? parameter)
        {
            SelectedProducts.Clear();

            // Update self and item removal.
            command_RemoveAllCart.InvokeCanExecuteChanged();
            command_RemoveFromCart.InvokeCanExecuteChanged();

            // Update Cash Values.
            Update_CashValues();
        }
        public bool Allow_RemoveAllCart(object? parameter)
        {
            return SelectedProducts.Count > 0;
        }

        #endregion

        /// <summary>
        /// Public ctor.
        /// </summary>
        public CartManager_ViewModel()
        {
            selectedProducts = new();

            command_AddToCart = new RelayCommand(Order_AddToCart, Allow_AddToCart);
            command_RemoveFromCart = new RelayCommand(Order_RemoveFromCart, Allow_RemoveFromCart);
            command_RemoveAllCart = new RelayCommand(Order_RemoveAllCart, Allow_RemoveAllCart);
        }

        public void Update_CashValues()
        {
            ulong sum_Prices = 0;
            foreach(var x in SelectedProducts)
            {
                sum_Prices += x.Price;
            }
            TotalPrice = sum_Prices;

            // TODO: Calculate Discount.
            ulong sum_Discount = 0;

            long pay = (long)sum_Prices - (long)sum_Discount;
            if (pay < 0)
                TotalPayment = 0;
            else
                TotalPayment = sum_Prices - sum_Discount;
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class FromDateTime_ToPersianLongDateString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = (DateTime) value;
            PersianCalendar pc = new PersianCalendar();

            string dayOfWeek = string.Empty;
            switch(pc.GetDayOfWeek(dt))
            {
                case DayOfWeek.Sunday: dayOfWeek = "یکشنبه"; break;
                case DayOfWeek.Monday: dayOfWeek = "دوشنبه"; break;
                case DayOfWeek.Tuesday: dayOfWeek = "سه‌شنبه"; break;
                case DayOfWeek.Wednesday: dayOfWeek = "چهارشنبه"; break;
                case DayOfWeek.Thursday: dayOfWeek = "پنج‌شنبه"; break;
                case DayOfWeek.Friday: dayOfWeek = "جمعه"; break;
                case DayOfWeek.Saturday: dayOfWeek = "شنبه"; break;
            }

            string monthName = string.Empty;
            switch(pc.GetMonth(dt))
            {
                case 1: monthName = "فروردین"; break;
                case 2: monthName = "اردیبهشت"; break;
                case 3: monthName = "خرداد"; break;
                case 4: monthName = "تیر"; break;
                case 5: monthName = "مرداد"; break;
                case 6: monthName = "شهریور"; break;
                case 7: monthName = "مهر"; break;
                case 8: monthName = "آبان"; break;
                case 9: monthName = "آذر"; break;
                case 10: monthName = "دی"; break;
                case 11: monthName = "بهمن"; break;
                case 12: monthName = "اسفند"; break;
            }

            return $"{dayOfWeek}، {pc.GetDayOfMonth(dt):00} {monthName} {pc.GetYear(dt):0000}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("امکان تبدیل رشته به تاریخ در این ماژول وجود ندارد.");
        }
    }

    [ValueConversion(typeof(string), typeof(BitmapSource))]
    public class FromString_To_Bitmap : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage bmpImg = new BitmapImage(new Uri((string)value, UriKind.RelativeOrAbsolute));
            return bmpImg;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("امکان تبدیل رشته به تصویر بیت‌مپ وجود ندارد.");
        }
    }

    [ValueConversion(typeof(ulong), typeof(string))]
    public class FromUint64_To_ThousandSeparatedString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{(ulong)value:N0}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
