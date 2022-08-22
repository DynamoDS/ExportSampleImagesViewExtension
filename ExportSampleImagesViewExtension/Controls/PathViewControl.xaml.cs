using System.Windows.Controls;
using System.Windows.Input;

namespace ExportSampleImages.Controls
{
    /// <summary>
    ///     Interaction logic for FolderPathViewControl.xaml
    /// </summary>
    public partial class FolderPathViewControl : UserControl
    {
        public FolderPathViewControl()
        {
            InitializeComponent();
        }

        private void PlaceholderTextBlock_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                this.Cursor = Cursors.Hand;
        }

        private void PlaceholderTextBlock_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            this.Cursor = null;
        }
    }
}