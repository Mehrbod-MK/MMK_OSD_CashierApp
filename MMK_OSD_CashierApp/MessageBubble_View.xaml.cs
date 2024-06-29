using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class MessageBubble_Result
    {
        public string dialogResult = "None";
        public string? response;
        public SecureString? password;
    }

    /// <summary>
    /// Interaction logic for MessageBubble_ViewModel.xaml
    /// </summary>
    public partial class MessageBubble_View : Window
    {
        public MessageBubble_View(
            string title = "عنوان پیام", 
            string text = "متن پیام", 
            string? defaultResponse = null,
            MessageBoxImage msgBubble_Icon = MessageBoxImage.None,
            string msgBubble_ImagePath = "../Resources/cashier.png",
            MessageBoxButton msgBubble_Buttons = MessageBoxButton.OK,
            bool isResponseAPassword = false)
        {
            InitializeComponent();

            text_Msg_Title.Text = title;
            text_Msg_Text.Text = text;
            if(defaultResponse != null)
            {
                textBox_Msg_Response.Text = defaultResponse;
                textBox_Msg_Response.Visibility = Visibility.Visible;
            }
            else
            {
                textBox_Msg_Response.Visibility = Visibility.Collapsed;
            }
            
            switch((int)msgBubble_Icon)
            {
                case 48:
                    image_MsgBox_Icon.Source = new BitmapImage(
                        new Uri(@"../Resources/ico_MsgBox_Warning.png", UriKind.Relative));
                    break;

                case 16:
                    image_MsgBox_Icon.Source = new BitmapImage(
                        new Uri(@"../Resources/ico_MsgBox_Error.png", UriKind.Relative));
                    break;

                default:
                    image_MsgBox_Icon.Source = new BitmapImage(
                        new Uri(@"../Resources/ico_MsgBox_Info.png", UriKind.Relative));
                    break;
            }

            image_MsgBox_Image.Source = new BitmapImage(
                new Uri(msgBubble_ImagePath, UriKind.Relative)
                );

            switch(msgBubble_Buttons)
            {
                case MessageBoxButton.OK:
                    button_1.Visibility = Visibility.Visible;
                    button_2.Visibility = Visibility.Hidden;
                    button_3.Visibility = Visibility.Hidden;
                    button_1.Content = "باشد";
                    button_1.Tag = "OK";
                    break;

                case MessageBoxButton.OKCancel:
                    button_1.Visibility = Visibility.Visible;
                    button_2.Visibility = Visibility.Visible;
                    button_3.Visibility = Visibility.Hidden;
                    button_1.Content = "باشد";
                    button_2.Content = "لغو";
                    button_1.Tag = "OK";
                    button_2.Tag = "Cancel";
                    break;

                case MessageBoxButton.YesNo:
                    button_1.Visibility = Visibility.Visible;
                    button_2.Visibility = Visibility.Visible;
                    button_3.Visibility = Visibility.Hidden;
                    button_1.Content = "بله";
                    button_2.Content = "خیر";
                    button_1.Tag = "Yes";
                    button_2.Tag = "No";
                    break;

                case MessageBoxButton.YesNoCancel:
                    button_1.Visibility = Visibility.Visible;
                    button_2.Visibility = Visibility.Visible;
                    button_3.Visibility = Visibility.Visible;
                    button_1.Content = "بله";
                    button_2.Content = "خیر";
                    button_3.Content = "لغو";
                    button_1.Tag = "Yes";
                    button_2.Tag = "No";
                    button_3.Tag = "Cancel";
                    break;
            }

            if(isResponseAPassword)
            {
                textBox_Msg_Response.Visibility = Visibility.Collapsed;
                textBox_Msg_Password.Visibility = Visibility.Visible;
            }
        }

        private string resultOfBubble = string.Empty;

        private void Buttons_MessageBubble_Clicked(object sender, RoutedEventArgs e)
        {
            var buttonObject = sender as Button;

            if (buttonObject == null)
                return;
            if (buttonObject.Tag.ToString() == null)
                return;

            resultOfBubble = buttonObject.Tag?.ToString() ?? "";

            // Close message bubble.
            this.Close();
        }

        public MessageBubble_Result DisplayMessageBubble()
        {
            // Show this dialog.
            this.ShowDialog();

            // Return the result afterwards.
            return new()
            {
                dialogResult = resultOfBubble,
                response = textBox_Msg_Response.Text,
                password = textBox_Msg_Password.SecurePassword,
            };
        }
    }
}
