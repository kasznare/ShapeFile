using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Windows;
using System.Windows.Input;

namespace DockingMap
{
    class Map : GMapControl
    {
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
    }
}
