using MMK_OSD_CashierApp.Helpers;
using MMK_OSD_CashierApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace MMK_OSD_CashierApp.Views
{
    /// <summary>
    /// Interaction logic for Window_InputForm.xaml
    /// </summary>
    public partial class Window_InputForm : Window
    {
        public Window_InputForm(InputForm_ViewModel vm_InputForm)
        {
            InitializeComponent();

            this.DataContext = vm_InputForm;

            // Bind window itself to its context.
            vm_InputForm.Window_InputForm = this;

            // Initialize form components.
            foreach(var field in vm_InputForm.FormFields)
            {
                TextBlock txtBlock = new TextBlock()
                {
                    FontSize = 17,
                    FontWeight = FontWeights.DemiBold,
                    Margin= new Thickness(5.0),
                };
                txtBlock.SetBinding(TextBlock.TextProperty, 
                    new Binding("Question") 
                    { 
                        UpdateSourceTrigger = 
                        UpdateSourceTrigger.PropertyChanged, 
                        Source = field 
                    });

                TextBox txtBox = new TextBox()
                {
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                };
                txtBox.SetBinding(TextBox.TextProperty, 
                    new Binding("DefaultResponse")
                {
                    UpdateSourceTrigger =
                    UpdateSourceTrigger.PropertyChanged,
                    Source = field
                });

                StackPanel_Fields.Children.Insert(StackPanel_Fields.Children.Count, txtBlock);
                StackPanel_Fields.Children.Insert(StackPanel_Fields.Children.Count, txtBox);
            }
        }

        private bool cancelClose = false;

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not InputForm_ViewModel vm_InputForm)
                return;

            //  Call evaluation function.
            vm_InputForm.IsEvaluationOK = vm_InputForm.Function_EvaluateFields.Invoke();
            cancelClose = !vm_InputForm.IsEvaluationOK;

            this.Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancelClose = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = cancelClose;

            if (e.Cancel)
            {
                MakeMessageBoxes.Display_Warning(
                    "لطفاً مقادیر فرم را به درستی و در فرمت درست وارد کنید.",
                    "هشدار",
                    MessageBoxButton.OK,
                    MessageBoxResult.OK
                    );

                cancelClose = false;
            }
        }
    }
}
