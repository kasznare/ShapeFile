using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using GeoAPI.Geometries;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using NetTopologySuite.Windows.Media;

namespace ShapefileEditor
{
    class GMapNTSGeometryMarker : GMapMarker, IShapable
    {
        private static GMapPathGeometryWriter wpfPathGeometryWriter = new GMapPathGeometryWriter();

        public List<PointLatLng> Points { get; set; }
        private IGeometry geometry;
        private PathGeometry geoPathGeometry;
        private PathGeometry displayPathGeometry;
        private Path path;

        public GMapNTSGeometryMarker(IGeometry geometry) : base(new PointLatLng())
        {
            Points = new List<PointLatLng>();
            Points.Add(new PointLatLng(0, 0));
            Points.Add(new PointLatLng(0, 0));
            Position = Points[0];

            this.geometry = geometry;
            geoPathGeometry = wpfPathGeometryWriter.ToShape(geometry) as PathGeometry;
            displayPathGeometry = geoPathGeometry.Clone();
        }

        public override void Clear()
        {
            base.Clear();
            Points.Clear();
        }

        /// <summary>
        /// creates path from list of points, for performance set addBlurEffect to false
        /// </summary>
        /// <param name="pl"></param>
        /// <returns></returns>
        public virtual Path CreatePath(List<Point> localPath, bool addBlurEffect)
        {
            GPoint offset = Map.FromLatLngToLocal(new PointLatLng());
            for (int f = 0; f < geoPathGeometry.Figures.Count; f++)
            {
                PathFigure figureGeo = geoPathGeometry.Figures[f];
                PathFigure figureDisplay = displayPathGeometry.Figures[f];
                Point point = figureGeo.StartPoint;
                GPoint gPoint = Map.FromLatLngToLocal(new PointLatLng(point.X, point.Y));
                figureDisplay.StartPoint = new Point(gPoint.X - offset.X, gPoint.Y - offset.Y);
                PathSegmentCollection segmentsGeo = figureGeo.Segments;
                PathSegmentCollection segmentsDisplay = figureDisplay.Segments;
                for (int s = 0; s < segmentsGeo.Count; s++)
                {
                    PointCollection pointsGeo = (segmentsGeo[s] as PolyLineSegment).Points;
                    PointCollection pointsDisplay = (segmentsDisplay[s] as PolyLineSegment).Points;
                    for (int p = 0; p < pointsGeo.Count; p++)
                    {
                        point = pointsGeo[p];
                        gPoint = Map.FromLatLngToLocal(new PointLatLng(point.X, point.Y));
                        pointsDisplay[p] = new Point(gPoint.X - offset.X, gPoint.Y - offset.Y);
                    }
                }
            }



            if (path == null)
            {
                path = new Path();
                {
                    // Specify the shape of the Path using the StreamGeometry.
                    //if (addBlurEffect)
                    //{
                    //    BlurEffect ef = new BlurEffect();
                    //    {
                    //        ef.KernelType = KernelType.Gaussian;
                    //        ef.Radius = 3.0;
                    //        ef.RenderingBias = RenderingBias.Performance;
                    //    }
                    //    path.Effect = ef;
                    //}
                    path.Stroke = Brushes.MidnightBlue;
                    path.StrokeThickness = 5;
                    path.StrokeLineJoin = PenLineJoin.Round;
                    path.StrokeStartLineCap = PenLineCap.Triangle;
                    path.StrokeEndLineCap = PenLineCap.Square;
                    path.Fill = Brushes.AliceBlue;
                    path.Opacity = 0.6;
                    path.IsHitTestVisible = false;
                }
            }
            path.Data = displayPathGeometry;
            return path;
        }

        //private IGeometry  
    }
}
