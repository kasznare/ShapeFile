using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for MapCanvas.xaml
    /// </summary>
    public partial class MapCanvas : Canvas
    {
        public MapCanvas(Map map)
        {
            Map = map;
            
            InitializeComponent();
            thumbStyle = FindResource("thumbStyle") as Style;

            DataContext = this;
            
            Map.PreviewMouseLeftButtonDown += Map_PreviewMouseLeftButtonDown;
            Map.PreviewMouseLeftButtonUp += Map_PreviewMouseLeftButtonUp;
            Map.PreviewMouseMove += Map_PreviewMouseMove;
            Map.OnMapZoomChanged += Map_OnMapZoomChanged;

            Marker = new GMapMarker(Map.MapProvider.Projection.Bounds.LocationTopLeft)
            {
                Shape = this,
                Offset = new Point(0, 0)
            };
        }


        #region Dependency properties

        public Map Map
        {
            get { return (Map)GetValue(MapProperty); }
            set { SetValue(MapProperty, value); }
        }
        public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(MapCanvas));

        private ObservableCollection<Layer> layers;
        public ObservableCollection<Layer> Layers
        {
            get { return layers; }
            set
            {
                layers = value;
                CollectionViewSource.GetDefaultView(layers).CurrentChanged += MapCanvas_CurrentChanged;
            }
        }

        #endregion Dependency properties


        #region Properties

        public GMapMarker Marker { get; internal set; }

        #endregion Properties


        #region Mouse handling

        bool leftClicking = false;
        bool leftClickingOnShape = false;
        

        private void Map_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            leftClicking = false;
        }

        private void Map_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (leftClicking && !leftClickingOnShape)
                RemoveThumbs();

            leftClicking = false;
        }

        private void Map_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            leftClicking = true;
        }

        private void Map_OnMapZoomChanged()
        {
            ScaleThumbs();
        }

        private void CanvasShape_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (leftClickingOnShape)
            {
                CanvasShape canvasShape = (CanvasShape)sender;
                CollectionViewSource.GetDefaultView(Layers).MoveCurrentTo(canvasShape.Layer);
                CollectionViewSource.GetDefaultView(canvasShape.Layer.Shapes).MoveCurrentTo(canvasShape.DataContext);

                RemoveThumbs();
                PlaceThumbs(canvasShape.DisplayGeometry);
            }
            leftClickingOnShape = false;
        }

        private void CanvasShape_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            leftClickingOnShape = true;
        }

        private void CanvasShape_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            leftClickingOnShape = false;
        }

        private void MapCanvas_CurrentChanged(object sender, EventArgs e)
        {
            RemoveThumbs();
        }

        #endregion Mouse handling


        #region Thumb handling

        private List<Thumb> allThumbs = new List<Thumb>();
        private Style thumbStyle;
        

        private void RemoveThumbs()
        {
            foreach (Thumb thumb in allThumbs)
            {
                Children.Remove(thumb);
            }
            allThumbs.Clear();
        }

        private void PlaceThumbs(PathGeometry pathGeometry)
        {
            double scale = Map.Scale;
            foreach (PathFigure figure in pathGeometry.Figures)
            {
                {
                    Thumb thumb = new Thumb();
                    SetLeft(thumb, figure.StartPoint.X * scale);
                    SetTop(thumb, figure.StartPoint.Y * scale);
                    thumb.Style = thumbStyle;
                    thumb.Tag = new ThumbTag()
                    {
                        Figure = figure,
                        IsStartPoint = true,
                        Scale = scale
                    };

                    Children.Add(thumb);
                    allThumbs.Add(thumb);
                }
                List<Thumb> thumbs = new List<Thumb>();
                foreach (Point point in (figure.Segments[0] as PolyLineSegment).Points)
                {
                    Thumb thumb = new Thumb();
                    SetLeft(thumb, point.X * scale);
                    SetTop(thumb, point.Y * scale);
                    thumb.Style = thumbStyle;
                    thumb.Tag = new ThumbTag()
                    {
                        Figure = figure,
                        IsStartPoint = false,
                        Scale = scale,
                        ThumbList = thumbs
                    };

                    Children.Add(thumb);
                    thumbs.Add(thumb);
                    allThumbs.Add(thumb);
                }
            }
        }

        private void ScaleThumbs()
        {
            double newScale = Map.Scale;
            foreach (Thumb thumb in allThumbs)
            {
                ThumbTag tag = (ThumbTag)thumb.Tag;
                double oldScale = tag.Scale;

                SetLeft(thumb, GetLeft(thumb) / oldScale * newScale);
                SetTop(thumb, GetTop(thumb) / oldScale * newScale);

                tag.Scale = newScale;
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = sender as Thumb;
            if (thumb != null)
            {
                double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
                double newTop = Canvas.GetTop(thumb) + e.VerticalChange;

                Canvas.SetLeft(thumb, newLeft);
                Canvas.SetTop(thumb, newTop);

                ThumbTag tag = (ThumbTag)thumb.Tag;
                if (tag.IsStartPoint)
                {
                    tag.Figure.StartPoint = new Point(newLeft / tag.Scale, newTop / tag.Scale);
                }
                else
                {
                    int index = tag.ThumbList.IndexOf(thumb);
                    (tag.Figure.Segments[0] as PolyLineSegment).Points[index] = new Point(newLeft / tag.Scale, newTop / tag.Scale);
                }
            }
        }

        #endregion Thumb handling
    }


    /// <summary>
    /// Tag class for polygon edit thumbs
    /// </summary>
    public class ThumbTag
    {
        public PathFigure Figure { get; set; }
        public bool IsStartPoint { get; set; }
        public double Scale { get; set; }
        public List<Thumb> ThumbList { get; set; }
    }


    //public class StrokeThicknessConverter : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        double product = 1.0;
    //        for (int i = 0; i < values.Length; i++)
    //        {
    //            try
    //            {
    //                product *= (double)values[i];
    //            }
    //            catch { }
    //        }
    //        return product;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    //public class ShapeItemStyleSelector : StyleSelector
    //{
    //    public override Style SelectStyle(object item, DependencyObject container)
    //    {
    //        MapCanvas canvas = container as MapCanvas;

    //        if (canvas != null && item != null)
    //        {
    //            MainWindow window = (MainWindow)Application.Current.MainWindow;
    //            if (canvas.Layer != CollectionViewSource.GetDefaultView(window.Layers).CurrentItem)
    //                return base.SelectStyle(item, container);

    //            if (item is ShapefileShape)
    //            {
    //                ShapefileShape shape = item as ShapefileShape;


    //                if (shape != CollectionViewSource.GetDefaultView(canvas.Layer).CurrentItem)
    //                    return base.SelectStyle(item, container);
    //                else
    //                    return canvas.FindResource("selectedStyle") as Style;
    //            }
    //        }
    //        return base.SelectStyle(item, container);
    //    }
    //}
}
