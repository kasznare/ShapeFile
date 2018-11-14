using GMap.NET.WindowsPresentation;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for MapCanvas.xaml
    /// </summary>
    public partial class MapCanvas : ItemsControl
    {
        public MapCanvas(Map map, Layer layer)
        {
            Map = map;
            Layer = layer;

            InitializeComponent();

            DataContext = this;
            //ItemsSource = Layer.Shapes;

            Marker = new GMapMarker(Map.MapProvider.Projection.Bounds.LocationTopLeft)
            {
                Shape = this,
                Offset = new Point(0, 0)
            };
        }


        
        public Map Map
        {
            get { return (Map)GetValue(MapProperty); }
            set { SetValue(MapProperty, value); }
        }
        public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(MapCanvas));

        public Layer Layer
        {
            get { return (Layer)GetValue(LayerProperty); }
            set { SetValue(LayerProperty, value); }
        }
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register("Layer", typeof(Layer), typeof(MapCanvas));

        public GMapMarker Marker { get; internal set; }
    }

    public class StrokeThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double product = 1.0;
            for (int i = 0; i < values.Length; i++)
            {
                try
                {
                    product *= (double)values[i];
                }
                catch { }
            }
            return product;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
