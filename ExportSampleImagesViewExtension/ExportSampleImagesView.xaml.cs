using System.Windows.Controls;

namespace ExportSampleImages
{
    /// <summary>
    ///     Interaction logic for ExportSampleImagesWindow.xaml
    /// </summary>
    public partial class ExportSampleImagesView : UserControl
    {
        public ExportSampleImagesView(ExportSampleImagesViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;
        }
    }
}