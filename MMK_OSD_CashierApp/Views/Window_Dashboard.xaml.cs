﻿using MMK_OSD_CashierApp.ViewModels;
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
    /// Interaction logic for Window_Dashboard.xaml
    /// </summary>
    public partial class Window_Dashboard : Window
    {
        public Window_Dashboard(Dashboard_ViewModel vm_Dashboard)
        {
            InitializeComponent();

            this.DataContext = vm_Dashboard;
        }
    }
}
