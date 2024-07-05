using MMK_OSD_CashierApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Data;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class CartManager_ViewModel : ViewModelBase
    {
        private Product? foundProduct;

        /// <summary>
        /// Found product, entered by its code.
        /// </summary>
        public Product? FoundProduct { get => foundProduct; set { SetProperty(ref foundProduct, value); } }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class FromDateTime_ToPersianLongDateString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = (DateTime) value;

            string dayOfWeek = string.Empty;
            switch(dt.DayOfWeek)
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
            switch(dt.Month)
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

            return $"{dayOfWeek}، {dt.Day:00} {monthName} {dt.Year:0000}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("امکان تبدیل رشته به تاریخ در این ماژول وجود ندارد.");
        }
    }
}
