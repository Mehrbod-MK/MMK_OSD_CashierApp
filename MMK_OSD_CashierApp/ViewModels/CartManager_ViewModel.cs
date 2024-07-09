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
using System.Windows.Documents;
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

        private float? funds_DiscountPercent { get; set; }

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

            // Check for product's quantity.
            Worker_ViewModel vm = new();
            BackgroundWorker bkgWorker_AddToCart = new() { WorkerReportsProgress = false, WorkerSupportsCancellation = false, };
            bkgWorker_AddToCart.DoWork += (sender, e) =>
            {
                try
                {
                    vm.ProgressState = "در حال استعلام موجودی کالا...";
                    var queryProductResult = DB._THROW_DBRESULT<Product?>(
                        MainWindow.db.db_Get_Product(FoundProduct.ProductID).Result
                        );

                    if (queryProductResult == null)
                        throw new NullReferenceException("کالا دیگر وجود ندارد یا توسط انبارداری حذف شده است.");

                    if(queryProductResult.Quantity == 0)
                    {
                        MakeMessageBoxes.Display_Error("موجودی کالا به اتمام رسیده است.",
                            "خطای عدم موجودی",
                            MessageBoxButton.OK, MessageBoxResult.OK);

                        return;
                    }

                    // Update product's quantity in DB.
                    queryProductResult.Quantity--;

                    var editQuantityProductResult = DB._THROW_DBRESULT<bool?>(
                        MainWindow.db.db_Update_Products(new() { queryProductResult }).Result
                        );
                    
                    // Everything was OK...
                    e.Result = true;
                }
                catch(Exception ex)
                {
                    MakeMessageBoxes.Display_Error_DB(ex);
                }
            };

            bkgWorker_AddToCart.RunWorkerCompleted += (sender, e) =>
            {
                if ((bool?)e.Result == true)
                {
                    // Update quantity view.
                    FoundProduct.Quantity--;

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
            };

            Dialog_Worker dlgWorker_AddToCart = new(bkgWorker_AddToCart, vm);
            dlgWorker_AddToCart.ShowDialog();
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

            // Revive products quantity.
            Worker_ViewModel vm = new();
            BackgroundWorker bkgWorker_RemoveFromCart = new() { WorkerReportsProgress = false, WorkerSupportsCancellation = false, };
            bkgWorker_RemoveFromCart.DoWork += (sender, e) =>
            {
                try
                {
                    vm.ProgressState = "در حال حذف...";

                    List<Product> updatedProducts = new List<Product>();

                    foreach (var prod in selectedItems)
                    {
                        var db_GetProduct = DB._THROW_DBRESULT<Product?>(
                            MainWindow.db.db_Get_Product(prod.ProductID).Result
                            ) ?? throw new NullReferenceException("عدم وجود تعریف کالا در انبار!");

                        var foundProduct = updatedProducts.Find(x => x.ProductID == db_GetProduct.ProductID);
                        if (foundProduct == null)
                        {
                            db_GetProduct.Quantity++;
                            updatedProducts.Add(db_GetProduct);
                        }
                        else
                        {
                            foundProduct.Quantity++;
                        }
                    }

                    var editQuantityProductResult = DB._THROW_DBRESULT<bool?>(
                        MainWindow.db.db_Update_Products(updatedProducts).Result
                        );

                    // Everything was OK...
                    e.Result = true;
                }
                catch (Exception ex)
                {
                    MakeMessageBoxes.Display_Error_DB(ex);
                }
            };

            bkgWorker_RemoveFromCart.RunWorkerCompleted += (sender, e) =>
            {
                if ((bool?)e.Result == true)
                {
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
            };

            Dialog_Worker dlgWorker_AddToCart = new(bkgWorker_RemoveFromCart, vm);
            dlgWorker_AddToCart.ShowDialog();
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

            if (txtBox_NationalID.Text.Length == 10)
            {
                try
                {
                    var mostCustomerPayment = DB._THROW_DBRESULT<ulong?>(
                        MainWindow.db.db_Get_MostRecent_Purchase_Payment_SYNC(txtBox_NationalID.Text)
                        );

                    // Calculate Discount.
                    if (mostCustomerPayment != null && funds_DiscountPercent != null)
                    {
                        ulong x1 = (ulong)mostCustomerPayment * (ulong)funds_DiscountPercent;
                        ulong x2 = x1 / 10000;
                        TotalDiscount = x2;
                    }
                    else
                        TotalDiscount = 0;

                    Update_CashValues();
                }
                catch(Exception ex)
                {
                    MakeMessageBoxes.Display_Error_DB(ex);
                }

                return true;
            }
            else
                return false;
        }

        private RelayCommand command_RemoveAllCart;
        public ICommand Command_RemoveAllCart => command_RemoveAllCart;
        public void Order_RemoveAllCart(object? parameter)
        {
            // Revive products quantity.
            Worker_ViewModel vm = new();
            BackgroundWorker bkgWorker_RemoveAllCart = new() { WorkerReportsProgress = false, WorkerSupportsCancellation = false, };
            bkgWorker_RemoveAllCart.DoWork += (sender, e) =>
            {
                try
                {
                    vm.ProgressState = "در حال حذف سبد...";

                    List<Product> updatedProducts = new List<Product>();

                    foreach (var prod in SelectedProducts)
                    {
                        var db_GetProduct = DB._THROW_DBRESULT<Product?>(
                            MainWindow.db.db_Get_Product(prod.ProductID).Result
                            ) ?? throw new NullReferenceException("عدم وجود تعریف کالا در انبار!");

                        var foundProduct = updatedProducts.Find(x => x.ProductID == db_GetProduct.ProductID);
                        if(foundProduct == null)
                        {
                            db_GetProduct.Quantity++;
                            updatedProducts.Add(db_GetProduct);
                        }
                        else
                        {
                            foundProduct.Quantity++;
                        }
                    }

                    var editQuantityProductResult = DB._THROW_DBRESULT<bool?>(
                        MainWindow.db.db_Update_Products(updatedProducts).Result
                        );

                    // Everything was OK...
                    e.Result = true;
                }
                catch (Exception ex)
                {
                    MakeMessageBoxes.Display_Error_DB(ex);
                }
            };

            bkgWorker_RemoveAllCart.RunWorkerCompleted += (sender, e) =>
            {
                if ((bool?)e.Result == true)
                {
                    SelectedProducts.Clear();

                    // Update Cash Values.
                    Update_CashValues();

                    // Update self and remove all.
                    command_RemoveFromCart.InvokeCanExecuteChanged();
                    command_RemoveAllCart.InvokeCanExecuteChanged();

                    command_FinalSubmit.InvokeCanExecuteChanged();
                }
            };

            Dialog_Worker dlgWorker_AddToCart = new(bkgWorker_RemoveAllCart, vm);
            dlgWorker_AddToCart.ShowDialog();
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

                e.Result = true;
            };

            worker_FinalSubmit.RunWorkerCompleted += (sender, e) =>
            {
                wnd_CartManager.IsEnabled = true;

                if(e.Result != null && (bool)e.Result == true)
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
        public CartManager_ViewModel(string cashier_NationalID, float? funds_DiscountPercent)
        {
            selectedProducts = new();

            this.cashier_NationalID = cashier_NationalID;
            this.funds_DiscountPercent = funds_DiscountPercent;

            command_AddToCart = new RelayCommand(Order_AddToCart, Allow_AddToCart);
            command_RemoveFromCart = new RelayCommand(Order_RemoveFromCart, Allow_RemoveFromCart);
            command_RemoveAllCart = new RelayCommand(Order_RemoveAllCart, Allow_RemoveAllCart);
            command_RegisterNewCustomer = new RelayCommand(Order_RegisterNewCustomer, Allow_RegisterNewCustomer);
            command_FinalSubmit = new RelayCommand(Order_FinalSubmit, Allow_FinalSubmit);
        }

        protected void Reset_ViewModel()
        {
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

            ulong sum_Discount = TotalDiscount;

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
            if (value == null)
                return DependencyProperty.UnsetValue;

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
            try
            {
                BitmapImage bmpImg = new BitmapImage(new Uri((string)value, UriKind.RelativeOrAbsolute));
                return bmpImg;
            }
            catch(Exception)
            {
                return DependencyProperty.UnsetValue;
            }
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
