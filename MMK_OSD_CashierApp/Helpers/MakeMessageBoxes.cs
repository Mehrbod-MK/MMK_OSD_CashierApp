using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;

namespace MMK_OSD_CashierApp.Helpers
{
    public static class MakeMessageBoxes
    {
        public static MessageBoxResult Display_Error(string text, string caption, MessageBoxButton buttons, MessageBoxResult defaultResponse)
        {
            return MessageBox.Show(
                text,
                caption,
                buttons,
                MessageBoxImage.Error,
                defaultResponse,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                );
        }

        public static MessageBoxResult Display_Warning(string text, string caption, MessageBoxButton buttons, MessageBoxResult defaultResponse)
        {
            return MessageBox.Show(
                text,
                caption,
                buttons,
                MessageBoxImage.Warning,
                defaultResponse,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                );
        }

        public static MessageBoxResult Display_Notification(string text, string caption, MessageBoxButton buttons, MessageBoxResult defaultResponse)
        {
            return MessageBox.Show(
                text,
                caption,
                buttons,
                MessageBoxImage.Information,
                defaultResponse,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                );
        }

        public static MessageBoxResult Display_Error_DB(Exception? ex)
        {
            if(ex == null)
            {
                return MessageBox.Show(
                "خطای ناشناخته در هنگام خواندن یا نوشتن در پایگاه داده رخ داد.\nلطفاً با راهبر سیستم تماس بگیرید.",
                "خطای ناشناخته پایگاه داده",
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
                );
            }
            else
            {
                return MessageBox.Show(
                "خطای ذیل در هنگام خواندن یا نوشتن در پایگاه داده رخ داد:\n\n" + ex.ToString(),
                "خطای ناشناخته پایگاه داده",
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.OK
                );
            }
        }
    }
}
