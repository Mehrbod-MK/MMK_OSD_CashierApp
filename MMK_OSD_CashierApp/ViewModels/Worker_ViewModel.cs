using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class Worker_ViewModel : ViewModelBase
    {
        private string _progressState = "...";
        private System.Windows.Media.Brush _progressColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));

        public string ProgressState
        {
            get => _progressState;
            set => SetProperty(ref _progressState, value);
        }

        public System.Windows.Media.Brush ProgressColor
        {
            get => _progressColor;
            set => SetProperty(ref _progressColor, value);
        }
    }
}
