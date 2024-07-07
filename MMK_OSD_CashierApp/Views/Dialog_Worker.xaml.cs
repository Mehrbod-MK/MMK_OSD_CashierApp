using MMK_OSD_CashierApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MMK_OSD_CashierApp
{
    /// <summary>
    /// Interaction logic for Dialog_Worker.xaml
    /// </summary>
    public partial class Dialog_Worker : Window
    {
        BackgroundWorker worker = new BackgroundWorker();

        bool cancelClose = true;

        public Dialog_Worker(BackgroundWorker job, Worker_ViewModel worker_VM)
        {
            // Set binding data context.
            this.DataContext = worker_VM;

            InitializeComponent();

            worker = job;

            // Check if worker does not support progress report.
            progressBar_Worker.IsIndeterminate = !worker.WorkerReportsProgress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Add handler for updating progress bar.
            worker.ProgressChanged += Worker_ProgressChanged;

            // Add handler for closing dialog.
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            // Begin work.
            worker.RunWorkerAsync();
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar_Worker.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            // Close dialog.
            cancelClose = false;
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = cancelClose;
        }
    }
}
