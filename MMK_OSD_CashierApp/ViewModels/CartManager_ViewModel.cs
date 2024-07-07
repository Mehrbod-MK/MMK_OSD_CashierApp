using MMK_OSD_CashierApp.Helpers;
using MMK_OSD_CashierApp.Models;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private string? customer_NationalID;

        public string? Customer_NationalID
        {
            get => customer_NationalID;
            set => SetProperty(ref customer_NationalID, value);
        }

        private string cashier_NationalID;
        public string Cashier_NationalID
        {
            get => cashier_NationalID;
            private set => SetProperty(ref cashier_NationalID, value);
        }

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

            command_FinalSubmit.InvokeCanExecuteChanged();

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

            command_FinalSubmit.InvokeCanExecuteChanged();
        }
        public bool Allow_RemoveFromCart(object? parameter)
        {
            if (parameter is not ListView listView_Cart)
                return false;

            return SelectedProducts.Count > 0;
        }

        private RelayCommand command_RegisterNewCustomer;
        public ICommand Command_RegisterNewCustomer => command_RegisterNewCustomer;
        private bool can_RegisterNewCustomer = false;
        public bool Can_RegisterNewCustomer
        {
            get => can_RegisterNewCustomer;
            private set => SetProperty(ref can_RegisterNewCustomer, value);
        }
        public void Order_RegisterNewCustomer(object? parameter)
        {
            if (parameter is not TextBox txtBox_NationalID)
                return;

            var newCustomer = MainWindow.db.db_Register_User(txtBox_NationalID.Text, DB.DB_Roles.DB_ROLE_Customer);

            if(newCustomer.result == DB.DBResultEnum.DB_OK)
            {
                if(newCustomer.returnValue == null)
                {
                    MessageBox.Show(
                        "مشتری قبلاً در سامانه ثبت نام شده است.",
                        "هشدار",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                        );

                    return;
                }


                if (newCustomer.returnValue is not User customer)
                {
                    MessageBox.Show("خطای ثبت مشتری در پایگاه داده!",
                        "خطای پایگاه داده",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    return;
                }

                MessageBox.Show(
                    $"ثبت نام مشتری با موفقیت انجام شد.\n\nکد ملی:\t\t{txtBox_NationalID.Text}\nرمز عبور =\t\t{txtBox_NationalID.Text}",
                    "موفقیت در ثبت نام مشتری!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Asterisk,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                    );
            }
            else if(newCustomer.result == DB.DBResultEnum.DB_ERROR)
            {
                if(newCustomer.returnValue is not Exception ex)
                {
                    MessageBox.Show(
                        "خطای ناشناخته.",
                        "خطا",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                        );

                    return;
                }

                MessageBox.Show(
                        "متأسفانه، خطای ذیل در ثبت نام مشتری به وجود آمد:\n\n" + ex.ToString(),
                        "خطای ثبت نام مشتری",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                        );
            }
        }
        public bool Allow_RegisterNewCustomer(object? parameter)
        {
            if (parameter is not TextBox txtBox_NationalID)
                return false;

            return txtBox_NationalID.Text.Length == 10;
        }

        private RelayCommand command_RemoveAllCart;
        public ICommand Command_RemoveAllCart => command_RemoveAllCart;
        public void Order_RemoveAllCart(object? parameter)
        {
            SelectedProducts.Clear();

            // Update self and item removal.
            command_RemoveAllCart.InvokeCanExecuteChanged();
            command_RemoveFromCart.InvokeCanExecuteChanged();

            command_FinalSubmit.InvokeCanExecuteChanged();

            // Update Cash Values.
            Update_CashValues();
        }
        public bool Allow_RemoveAllCart(object? parameter)
        {
            return SelectedProducts.Count > 0;
        }


        private RelayCommand command_FinalSubmit;
        public ICommand Command_FinalSubmit => command_FinalSubmit;
        public void Order_FinalSubmit(object? parameter)
        {
            if (parameter is not Window wnd_CartManager)
                return;

            BackgroundWorker worker_FinalSubmit = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };

            Worker_ViewModel vm_Worker_FinalSubmit = new();

            worker_FinalSubmit.DoWork += (sender, e) =>
            {
                string customer_NationalID = cashier_NationalID;

                // Hide window.
                Application.Current.Dispatcher.Invoke(() => wnd_CartManager.IsEnabled = false);

                // Check customer availability.
                vm_Worker_FinalSubmit.ProgressState = "در حال بررسی اعتبار کد ملی مشتری...";
                if (Customer_NationalID == null)
                {
                    if (MessageBox.Show(
                        caption: "هشدار",
                        messageBoxText: "کد ملی مشتری وارد نشده است، در این صورت، تخفیف به ایشان تعلق نمی‌گیرد و تراکنش خرید به نام خود صندوقدار ثبت می‌گردد. آیا مطمئن هستید؟",
                        button: MessageBoxButton.YesNo,
                        icon: MessageBoxImage.Warning,
                        defaultResult: MessageBoxResult.No,
                        options: MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                        ) == MessageBoxResult.No)
                        return;
                }
                else
                {
                    var foundCustomer = /*Task.Run(() =>*/
                        MainWindow.db.db_Get_User(Customer_NationalID).Result;
                   /* ).Result*/

                    if (foundCustomer.result == DB.DBResultEnum.DB_OK)
                    {
                        if (foundCustomer.returnValue == null)
                        {
                            if (MakeMessageBoxes.Display_Warning(
                                "کد ملی وارد شده در پایگاه داده یافت نشد. آیا مطمئن هستید که بدون کد ملی درست می‌خواهید تراکنش خرید را ثبت کنید؟ در این صورت، تخفیف به مشتری تعلق نگرفته و تراکنش به نام صندوقدار ثبت خواهد شد.\n\nاز این بابت مطمئن هستید؟",
                                "هشدار عدم اعتبار کد ملی مشتری",
                                MessageBoxButton.YesNo,
                                MessageBoxResult.No
                                ) == MessageBoxResult.No)
                                return;
                        }

                        // Get customer's national ID.
                        if (foundCustomer.returnValue is not User customer)
                            return;
                        customer_NationalID = customer.NationalID;
                    }
                    else
                    {
                        MakeMessageBoxes.Display_Error_DB(foundCustomer.returnValue as Exception);
                        return;
                    }
                }

                // Begin purchase transaction submission in DB.
                vm_Worker_FinalSubmit.ProgressState = "در حال ثبت تراکنش خرید...";
                DateTime dt = DateTime.Now;
                string sqlDateTimeStr = DB.Convert_FromDateTime_ToSQLDateTimeString(dt);
                var db_WritePurchase = MainWindow.db.sql_Execute_NonQuery
                ($"INSERT INTO {DB.DB_TABLE_NAME_PURCHASES} " +
                $"(Customer_NationalID, DateTimeSubmitted, SubmittedBy_NationalID, " +
                $"Num_ProductsPurchased, Total_Price, Total_Discount, Total_Payment) VALUES (" +
                $"\'{customer_NationalID}\', \'{sqlDateTimeStr}\', \'{cashier_NationalID}\'," +
                $"{SelectedProducts.Count}, {total_Price}, {total_Discount}, {total_Payment});").Result;

                vm_Worker_FinalSubmit.ProgressState = "در حال بررسی تراکنش خرید...";
                if(db_WritePurchase.result == DB.DBResultEnum.DB_ROLLBACKED_TRANSACTION)
                {
                    MakeMessageBoxes.Display_Error("به علت خطایی ناشناخته، تراکنش توسط DBMS ثبت نشد. لطفاً لحظاتی بعد مجدداً تلاش کنید.",
                        "خطای ثبت تراکنش", MessageBoxButton.OK, MessageBoxResult.OK);
                    return;
                }
                else if(db_WritePurchase.result == DB.DBResultEnum.DB_ERROR)
                {
                    MakeMessageBoxes.Display_Error_DB(db_WritePurchase.returnValue as Exception);
                    return;
                }

                // Successful submission.
                MakeMessageBoxes.Display_Notification(
                    "تراکنش با موفقیت در پایگاه داده ثبت شد.",
                    "موفقیت",
                    MessageBoxButton.OK,
                    MessageBoxResult.OK);

            };

            worker_FinalSubmit.RunWorkerCompleted += (sender, e) =>
            {
                wnd_CartManager.IsEnabled = true;

                Reset_ViewModel();
            };

            Dialog_Worker workerDlg_FinalSubmit = new Dialog_Worker(worker_FinalSubmit, vm_Worker_FinalSubmit);
            workerDlg_FinalSubmit.ShowDialog();
        }
        public bool Allow_FinalSubmit(object? parameter)
        {
            return SelectedProducts.Count > 0;
        }

        #endregion

        /// <summary>
        /// Public ctor.
        /// </summary>
        public CartManager_ViewModel(string cashier_NationalID)
        {
            selectedProducts = new();

            this.cashier_NationalID = cashier_NationalID;

            command_AddToCart = new RelayCommand(Order_AddToCart, Allow_AddToCart);
            command_RemoveFromCart = new RelayCommand(Order_RemoveFromCart, Allow_RemoveFromCart);
            command_RemoveAllCart = new RelayCommand(Order_RemoveAllCart, Allow_RemoveAllCart);
            command_RegisterNewCustomer = new RelayCommand(Order_RegisterNewCustomer, Allow_RegisterNewCustomer);
            command_FinalSubmit = new RelayCommand(Order_FinalSubmit, Allow_FinalSubmit);
        }

        protected void Reset_ViewModel()
        {
            FoundProduct = null;
            Customer_NationalID = null;
            SelectedProducts.Clear();

            Update_CashValues();

            command_AddToCart.InvokeCanExecuteChanged();
            command_RemoveFromCart.InvokeCanExecuteChanged();
            command_RemoveAllCart.InvokeCanExecuteChanged();
            command_RegisterNewCustomer.InvokeCanExecuteChanged();
            command_FinalSubmit.InvokeCanExecuteChanged();
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
