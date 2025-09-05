using System.Windows.Controls;

namespace YMM_REC_Plugin
{
    public partial class ToolView : UserControl
    {
        public ToolView()
        {
            InitializeComponent();
            this.DataContext = new ToolViewModel();
        }
    }
}
