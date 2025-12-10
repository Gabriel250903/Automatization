using Automatization.ViewModels;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class RoadmapWindow : FluentWindow
    {
        public RoadmapWindow()
        {
            InitializeComponent();
            DataContext = new RoadmapViewModel();
        }
    }
}
