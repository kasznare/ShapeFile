using GeoAPI.Geometries;
using GMap.NET;
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
        public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(CanvasShape));

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
        
        public Transform Transform
        {
            get { return (Transform)GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }
        public static readonly DependencyProperty TransformProperty = DependencyProperty.Register("Transform", typeof(Transform), typeof(CanvasShape), new PropertyMetadata(Transform.Identity, TransformPropertyChanged));

        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CanvasShape cs = ((CanvasShape)d);
            cs.DisplayGeometry.Transform = cs.Transform;
        }

        public Layer Layer
        {
            get { return (Layer)GetValue(LayerProperty); }
            set { SetValue(LayerProperty, value); }
        }
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register("Layer", typeof(Layer), typeof(CanvasShape), new PropertyMetadata(null));



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
    }
}