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

namespace ShapefileEditor
{
    /// <summary>
    /// Interaction logic for CanvasShape.xaml
    /// </summary>
    public partial class CanvasShape : UserControl
    {
        private static GMapPathGeometryWriter pathGeometryWriter = new GMapPathGeometryWriter();
        private static GMapGeometryReader geometryReader = new GMapGeometryReader(new NetTopologySuite.Geometries.GeometryFactory());

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


        private bool creatingNewShape = false;

        private PathGeometry geoPathGeometry;

        public PathGeometry DisplayGeometry
        {
            get { return (PathGeometry)GetValue(DisplayGeometryProperty); }
            set { SetValue(DisplayGeometryProperty, value); }
        }
        public static DependencyProperty DisplayGeometryProperty = DependencyProperty.Register("DisplayGeometry", typeof(PathGeometry), typeof(CanvasShape), new PropertyMetadata(new PathGeometry()));
        
        public Map Map
        {
            get { return (Map)GetValue(MapProperty); }
            set { SetValue(MapProperty, value); }
        }
        public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(CanvasShape), new PropertyMetadata(new PropertyChangedCallback(MapChanged)));

        private static void MapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CanvasShape cs = ((CanvasShape)d);
            if (cs.creatingNewShape)
            {
                Map map = (Map)e.NewValue;
                RectLatLng viewArea = map.ViewArea;
                switch (cs.ShapeType)
                {
                    case AllowedShapeType.Point:
                        //cs.Geometry = geometryReader.Read(new PathGeometry();
                        break;
                    case AllowedShapeType.Line:
                        Point left = new Point(viewArea.Lat - viewArea.HeightLat * 0.5, viewArea.Lng + viewArea.WidthLng * 0.4);
                        Point right = new Point(viewArea.Lat - viewArea.HeightLat * 0.5, viewArea.Lng + viewArea.WidthLng * 0.6);
                        cs.Geometry = geometryReader.Read(new LineGeometry(left, right));
                        break;
                    case AllowedShapeType.Polygon:
                        Point topLeft = new Point(viewArea.Lat - viewArea.HeightLat * 0.6, viewArea.Lng + viewArea.WidthLng * 0.4);
                        Size size = new Size(viewArea.HeightLat * 0.2, viewArea.WidthLng * 0.2);
                        cs.Geometry = geometryReader.Read(new RectangleGeometry(new Rect(topLeft, size)));
                        break;
                }
                cs.DisplayGeometry.Transform = cs.Transform;
            }
        }
        
        public AllowedShapeType ShapeType
        {
            get { return (AllowedShapeType)GetValue(ShapeTypeProperty); }
            set { SetValue(ShapeTypeProperty, value); }
        }
        public static readonly DependencyProperty ShapeTypeProperty = DependencyProperty.Register("ShapeType", typeof(AllowedShapeType), typeof(CanvasShape), new PropertyMetadata(AllowedShapeType.Line));



        private bool isCommiting = false;

        public IGeometry Geometry
        {
            get { return (IGeometry)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(IGeometry), typeof(CanvasShape), new PropertyMetadata(null, new PropertyChangedCallback(GeometryPropertyChanged), new CoerceValueCallback(GeometryCoerceValueCallback)));

        private static object GeometryCoerceValueCallback(DependencyObject d, object baseValue)
        {
            CanvasShape cs = ((CanvasShape)d);
            if (baseValue == null)
            {
                cs.creatingNewShape = true;
                return null;
            }
            cs.creatingNewShape = false;
            return baseValue;
        }

        private static void GeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                Console.WriteLine("null");
                return;
            }
            CanvasShape cs = ((CanvasShape)d);
            if (!cs.isCommiting)
            {
                cs.geoPathGeometry = pathGeometryWriter.ToShape(e.NewValue as IGeometry) as PathGeometry;
                cs.path.Data = cs.DisplayGeometry = cs.geoPathGeometry.Clone();
                cs.RebuildShape();
            }
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
            if (!cs.DisplayGeometry.IsFrozen)
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
        private static Point FromLatLngToCanvasMercator(Point latLngPoint)
        {
            double x = (latLngPoint.Y + 180.0) * xFactor;
            double sinLatitude = Math.Sin(latLngPoint.X * deg2Rad);
            double y = 128.0 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) * yFactor;
            return new Point(x, y);
        }

        private static double invXFactor = 1.0 / xFactor;
        private static double invYFactor = 0.5 / yFactor;
        private static double rad2DegTimes2 = 360.0 / Math.PI;
        private static double piOver4 = Math.PI * 0.25;
        private static Point FromCanvasMercatorToLatLng(Point canvasMercator)
        {
            return new Point(
                (Math.Atan(Math.Exp((128.0 - canvasMercator.Y) * invYFactor)) - piOver4) * rad2DegTimes2, 
                canvasMercator.X * invXFactor - 180.0);
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

        public void CommitChanges()
        {
            isCommiting = true;
            geoPathGeometry = DisplayGeometry.Clone();
            for (int f = 0; f < DisplayGeometry.Figures.Count; f++)
            {
                PathFigure figureGeo = geoPathGeometry.Figures[f];
                PathFigure figureDisplay = DisplayGeometry.Figures[f];
                figureGeo.StartPoint = FromCanvasMercatorToLatLng(figureDisplay.StartPoint);
                PathSegmentCollection segmentsGeo = figureGeo.Segments;
                PathSegmentCollection segmentsDisplay = figureDisplay.Segments;
                for (int s = 0; s < segmentsDisplay.Count; s++)
                {
                    PointCollection pointsGeo = (segmentsGeo[s] as PolyLineSegment).Points;
                    PointCollection pointsDisplay = (segmentsDisplay[s] as PolyLineSegment).Points;
                    for (int p = 0; p < pointsDisplay.Count; p++)
                    {
                        pointsGeo[p] = FromCanvasMercatorToLatLng(pointsDisplay[p]);
                    }
                }
            }
            Geometry = geometryReader.Read(geoPathGeometry);
            isCommiting = false;
        }
    }
}