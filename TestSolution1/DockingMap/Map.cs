using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace DockingMap
{
    public class Map : GMapControl
    {
        public Map()
        {
            IgnoreMarkerOnMouseWheel = true;
            OnMapZoomChanged += Map_OnMapZoomChanged;
        }

        private void Map_OnMapZoomChanged()
        {
            SetValue(ScaleProperty, MapProvider.Projection.FromLatLngToPixel(0, 0, (int)Zoom).X / 128.0);
            SetValue(InverseScaleProperty, 1 / Scale);
        }

        public static readonly DependencyProperty MouseLatLngPositionProperty = DependencyProperty.Register("MouseLatLngPosition", typeof(PointLatLng), typeof(Map));
        public PointLatLng MouseLatLngPosition
        {
            get { return (PointLatLng)GetValue(MouseLatLngPositionProperty); }
            set { SetValue(MouseLatLngPositionProperty, value); MouseLatLngPositionText = PointLatLngFormatter.Format(value); }
        }

        public static readonly DependencyProperty MouseLatLngPositionTextProperty = DependencyProperty.Register("MouseLatLngPositionText", typeof(string), typeof(Map));
        public string MouseLatLngPositionText
        {
            get { return (string)GetValue(MouseLatLngPositionTextProperty); }
            set { SetValue(MouseLatLngPositionTextProperty, value); }
        }

        public PointLatLng FromLocalToLatLng(Point p)
        {
            return FromLocalToLatLng((int)p.X, (int)p.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MouseLatLngPosition = FromLocalToLatLng(Mouse.GetPosition(this));
        }

        private ZoomConverter _zoomConverter;

        public ZoomConverter ZoomConverter
        {
            get
            {
                if (_zoomConverter == null)
                    _zoomConverter = new ZoomConverter(this);
                return _zoomConverter;
            }
            set { _zoomConverter = value; }
        }

        
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register("Scale", typeof(double), typeof(Map));
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            private set { SetValue(ScaleProperty, value); }
        }

        public static readonly DependencyProperty InverseScaleProperty = DependencyProperty.Register("InverseScale", typeof(double), typeof(Map));
        public double InverseScale
        {
            get { return (double)GetValue(InverseScaleProperty); }
            private set { SetValue(InverseScaleProperty, value); }
        }
    }


    [ValueConversion(typeof(int), typeof(double))]
    public class ZoomConverter : MarkupExtension, IValueConverter
    {
        private Map map;
        public ZoomConverter(Map map)
        {
            this.map = map;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PureProjection proj = map.MapProvider.Projection;
            GSize size1 = proj.GetTileMatrixSizeXY(1);
            GSize sizeCurrent = proj.GetTileMatrixSizeXY((int)(double)value);
            return sizeCurrent.Width / (double)size1.Width * 0.001;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //return (int)Math.Log((double)value / 0.00001, 2);
            return DependencyProperty.UnsetValue;
        }
    }
}
