using GeoAPI.Geometries;
using GMap.NET;
using NetTopologySuite.Windows.Media;
using System;
using System.Collections.Generic;
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

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for CanvasShape.xaml
    /// </summary>
    public partial class CanvasShape : UserControl
    {
        private static GMapPathGeometryWriter pathGeometryWriter = new GMapPathGeometryWriter();

        public CanvasShape()
        {
            InitializeComponent();
            //DataContext = this;
        }

        public CanvasShape(Map map, IGeometry geometry)
        {
            InitializeComponent();
            Map = map;
            Geometry = geometry;
            RebuildShape();
        }


        private PathGeometry geoPathGeometry;

        public PathGeometry DisplayGeometry
        {
            get { return (PathGeometry)GetValue(DisplayGeometryProperty); }
            set { SetValue(DisplayGeometryProperty, value); }
        }
        public static readonly DependencyProperty DisplayGeometryProperty = DependencyProperty.Register("DisplayGeometry", typeof(PathGeometry), typeof(CanvasShape));
        
        public Map Map
        {
            get { return (Map)GetValue(MapProperty); }
            set { SetValue(MapProperty, value); }
        }
        public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(CanvasShape)/*, new PropertyMetadata(null)*/);

        public IGeometry Geometry
        {
            get { return (IGeometry)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(IGeometry), typeof(CanvasShape), new PropertyMetadata(null, new PropertyChangedCallback(GeometryPropertyChanged)));
        
        private static void GeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CanvasShape cs = ((CanvasShape)d);
            cs.geoPathGeometry = pathGeometryWriter.ToShape(e.NewValue as IGeometry) as PathGeometry;
            cs.path.Data = cs.DisplayGeometry = cs.geoPathGeometry.Clone();
            cs.RebuildShape();
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(CanvasShape));

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(CanvasShape));

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CanvasShape));



        private static double deg2Rad = Math.PI / 180.0;
        private static double inv4PI = 0.25 / Math.PI;
        private static double xFactor = 256.0 / 360.0;
        private static double yFactor = 256.0 * inv4PI;
        private Point FromLatLngToCanvasMercator(Point latLngPoint)
        {
            double x = (latLngPoint.Y + 180.0) * xFactor;
            double sinLatitude = Math.Sin(latLngPoint.X * deg2Rad);
            double y = 128 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) * yFactor;
            return new Point(x, y);
        }

        //private Point FromLatLngToCanvas(Point latLngPoint)
        //{
        //    GPoint gPoint = Map.MapProvider.Projection.FromLatLngToPixel(latLngPoint.X, latLngPoint.Y, 6);
        //    return new Point(gPoint.X/16.0, gPoint.Y/16.0);
        //}

        public void RebuildShape()
        {
            for (int f = 0; f < geoPathGeometry.Figures.Count; f++)
            {
                PathFigure figureGeo = geoPathGeometry.Figures[f];
                PathFigure figureDisplay = DisplayGeometry.Figures[f];
                figureDisplay.StartPoint = FromLatLngToCanvasMercator(figureGeo.StartPoint);
                PathSegmentCollection segmentsGeo = figureGeo.Segments;
                PathSegmentCollection segmentsDisplay = figureDisplay.Segments;
                for (int s = 0; s < segmentsGeo.Count; s++)
                {
                    PointCollection pointsGeo = (segmentsGeo[s] as PolyLineSegment).Points;
                    PointCollection pointsDisplay = (segmentsDisplay[s] as PolyLineSegment).Points;
                    for (int p = 0; p < pointsGeo.Count; p++)
                    {
                        pointsDisplay[p] = FromLatLngToCanvasMercator(pointsGeo[p]);
                    }
                }
            }
        }
















        bool clicking = false;
        private void path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clicking = true;
        }
        private void path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (clicking)
            {
                e.Handled = true;


                //TODO: clicking handling
            }
            clicking = false;
        }
        private void path_MouseMove(object sender, MouseEventArgs e)
        {
            clicking = false;
        }
    }
}