using MergeNow.Services;
using MergeNow.ViewModels;
using System.Windows.Controls;

namespace MergeNow.Views
{
    public partial class MergeNowSectionControl : UserControl
    {
        public MergeNowSectionControl()
        {
            InitializeComponent();
            DataContext = new MergeNowSectionViewModel(new MergeNowService());
        }
    }
}
