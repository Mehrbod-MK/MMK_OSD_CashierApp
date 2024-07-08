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
    }
}
