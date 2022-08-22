using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Dynamo.Utilities;
using ExportSampleImages.Properties;

namespace ExportSampleImages.Controls
{
    /// <summary>
    ///     Interaction logic for GraphViewControl.xaml
    /// </summary>
    public partial class GraphViewControl : UserControl
    {
        public GraphViewControl()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    ///     Converts null or empty string to Visibility Collapsed
    /// </summary>
    public class BooleanToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return ResourceUtilities.ConvertToImageSource(Resources.Progress_circle);

            var condition = (bool) value;
            if (condition) return ResourceUtilities.ConvertToImageSource(Resources.checkmark);

            return ResourceUtilities.ConvertToImageSource(Resources.Progress_circle);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}