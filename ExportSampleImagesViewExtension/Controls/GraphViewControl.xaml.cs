using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreNodeModels.Properties;
using Dynamo.Utilities;

namespace ExportSampleImagesViewExtension.Controls
{
    /// <summary>
    /// Interaction logic for GraphViewControl.xaml
    /// </summary>
    public partial class GraphViewControl : UserControl
    {
        public GraphViewControl()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Converts null or empty string to Visibility Collapsed 
    /// </summary>
    public class BooleanToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return ResourceUtilities.ConvertToImageSource(Properties.Resources.Progress_circle);

            bool condition = (bool)value;
            if (condition)
            {
                return ResourceUtilities.ConvertToImageSource(Properties.Resources.checkmark);
            }

            return ResourceUtilities.ConvertToImageSource(Properties.Resources.Progress_circle);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
