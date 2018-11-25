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

namespace ShapefileEditor
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


        private List<CanvasShape> selectedShapes = new List<CanvasShape>();

        public void CommitAll()
        {
            foreach (CanvasShape cs in selectedShapes)
            {
                cs.CommitChanges();
            }
        }

        private void Deselect()
        {
            CommitAll();
            RemoveThumbs();
            selectedShapes.Clear();
        }


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
                Deselect();

            leftClicking = false;
        }

        private void Map_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            leftClicking = true;
        }

        private void Map_OnMapZoomChanged()
        {
            Console.WriteLine(Map.Zoom);
            ScaleThumbs();
        }

        private void CanvasShape_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (leftClickingOnShape)
            {
                CanvasShape canvasShape = (CanvasShape)sender;
                CollectionViewSource.GetDefaultView(Layers).MoveCurrentTo(canvasShape.Layer);
                CollectionViewSource.GetDefaultView(canvasShape.Layer.Shapes).MoveCurrentTo(canvasShape.DataContext);

                Deselect();
                selectedShapes.Add(canvasShape);
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
            Deselect();
        }

        private void CanvasShape_Unloaded(object sender, RoutedEventArgs e)
        {
            //selectedShapes.Remove((CanvasShape)sender);
            Deselect();
        }

        #endregion Mouse handling


        #region Thumb handling

        private List<List<MoveThumb>> allMoveThumbs = new List<List<MoveThumb>>();


        private void RemoveThumbs()
        {
            foreach (List<MoveThumb> thumbList in allMoveThumbs)
            {
                foreach (MoveThumb thumb in thumbList)
                {
                    Children.Remove(thumb.PrevInsertThumb);
                    Children.Remove(thumb.NextInsertThumb);
                    Children.Remove(thumb);
                }
            }
            allMoveThumbs.Clear();
        }

        private void PlaceThumbs(PathGeometry pathGeometry)
        {
            double scale = Map.Scale;
            foreach (PathFigure figure in pathGeometry.Figures)
            {
                PointCollection points = (figure.Segments[0] as PolyLineSegment).Points;
                List<MoveThumb> thumbs = new List<MoveThumb>();
                allMoveThumbs.Add(thumbs);
                List<InsertThumb> insertThumbs = new List<InsertThumb>();


                for (int i = 0; i < points.Count; i++)
                {
                    MoveThumb thumb = new MoveThumb(this, figure, i, scale, thumbs);
                    thumbs.Add(thumb);
                }

                for (int i = 1; i < thumbs.Count - 1; i++)
                {
                    MoveThumb thumb = thumbs[i];
                    thumb.PrevThumb = thumbs[i - 1];
                    thumb.NextThumb = thumbs[i + 1];

                    InsertThumb prevInsertThumb = new InsertThumb(this, figure, i - 1, i, scale, thumbs);
                    prevInsertThumb.PrevThumb = thumb.PrevThumb;
                    prevInsertThumb.NextThumb = thumb;
                    insertThumbs.Add(prevInsertThumb);

                    thumb.PrevInsertThumb = prevInsertThumb;
                    thumb.PrevThumb.NextInsertThumb = prevInsertThumb;
                }

                MoveThumb startThumb = thumbs[0];
                MoveThumb lastThumb = thumbs[thumbs.Count-1];
                startThumb.NextThumb = thumbs[1];
                startThumb.NextInsertThumb = startThumb.NextThumb.PrevInsertThumb;
                lastThumb.PrevThumb = thumbs[thumbs.Count - 2];
                lastThumb.PrevInsertThumb = lastThumb.PrevThumb.NextInsertThumb;

                InsertThumb lastPrevInsertThumb = new InsertThumb(this, figure, thumbs.Count - 2, thumbs.Count - 1, scale, thumbs);
                lastPrevInsertThumb.PrevThumb = lastThumb.PrevThumb;
                lastPrevInsertThumb.NextThumb = lastThumb;
                insertThumbs.Add(lastPrevInsertThumb);

                lastThumb.PrevInsertThumb = lastPrevInsertThumb;
                lastThumb.PrevThumb.NextInsertThumb = lastPrevInsertThumb;

                if (figure.IsClosed)
                {
                    startThumb.PrevThumb = lastThumb;
                    lastThumb.NextThumb = startThumb;

                    InsertThumb insertThumb = new InsertThumb(this, figure, thumbs.Count - 1, 0, scale, thumbs);
                    insertThumb.PrevThumb = lastThumb;
                    insertThumb.NextThumb = startThumb;
                    insertThumbs.Add(insertThumb);

                    startThumb.PrevInsertThumb = insertThumb;
                    lastThumb.NextInsertThumb = insertThumb;
                }

                foreach (MoveThumb thumb in thumbs)
                {
                    Children.Add(thumb);
                }
                foreach (InsertThumb thumb in insertThumbs)
                {
                    Children.Add(thumb);
                }
            }
        }

        private void ScaleThumbs()
        {
            double newScale = Map.Scale;
            foreach (List<MoveThumb> thumbs in allMoveThumbs)
            {
                foreach (MoveThumb thumb in thumbs)
                {
                    thumb.Scale = newScale;
                }
            }
        }

        #endregion Thumb handling
    }


    public class MoveThumb : Thumb
    {
        public MoveThumb(MapCanvas parent, PathFigure figure, int index, double scale, List<MoveThumb> thumbList/*, bool isDragging = false*/)
        {
            ParentCanvas = parent;

            ThumbList = thumbList;

            Figure = figure;
            this.scale = scale;
            PointCollection points = (figure.Segments[0] as PolyLineSegment).Points;
            Point position = points[index];
            Canvas.SetLeft(this, position.X * scale);
            Canvas.SetTop(this, position.Y * scale);
            if (index == 0)
                figure.StartPoint = position;

            DragDelta += OnDragDelta;
            //IsDragging = isDragging;
            //if (isDragging)
            //    Mouse.Capture(this, CaptureMode.);
            //Console.WriteLine(isDragging);
        }
        

        public MapCanvas ParentCanvas { get; set; }
        
        public List<MoveThumb> ThumbList { get; set; }
        public MoveThumb PrevThumb { get; set; }
        public InsertThumb PrevInsertThumb { get; set; }
        public InsertThumb NextInsertThumb { get; set; }
        public MoveThumb NextThumb { get; set; }

        public PathFigure Figure { get; set; }

        private double scale = 1;
        public double Scale
        {
            get { return scale; }
            set
            {
                if (value != scale)
                {
                    double zoomFactor = value / scale;
                    Canvas.SetLeft(this, Canvas.GetLeft(this) * zoomFactor);
                    Canvas.SetTop(this, Canvas.GetTop(this) * zoomFactor);
                    scale = value;

                    if (PrevInsertThumb != null) PrevInsertThumb.Scale = scale;
                    if (NextInsertThumb != null) NextInsertThumb.Scale = scale;
                }
            }
        }

        public int GetIndex()
        {
            return ThumbList.IndexOf(this);
        }

        public void DeleteVertex()
        {
            PointCollection points = (Figure.Segments[0] as PolyLineSegment).Points;
            if (points.Count > 2)
            {
                int index = GetIndex();
                points.RemoveAt(index);
                ThumbList.RemoveAt(index);

                if (index == 0)
                    Figure.StartPoint = points[0];

                //MapCanvas parent = ((MapCanvas)Parent);

                if (PrevThumb == null)
                {
                    NextThumb.PrevThumb = null;
                    NextThumb.PrevInsertThumb = null;
                }
                else if (NextThumb == null)
                {
                    PrevThumb.NextThumb = null;
                    PrevThumb.NextInsertThumb = null;
                }
                else
                {
                    PrevThumb.NextThumb = NextThumb;
                    NextThumb.PrevThumb = PrevThumb;

                    InsertThumb newInsertThumb = new InsertThumb(ParentCanvas, Figure, PrevThumb.GetIndex(), NextThumb.GetIndex(), Scale, ThumbList);
                    newInsertThumb.PrevThumb = PrevThumb;
                    newInsertThumb.NextThumb = NextThumb;
                    PrevThumb.NextInsertThumb = newInsertThumb;
                    NextThumb.PrevInsertThumb = newInsertThumb;
                    ParentCanvas.Children.Add(newInsertThumb);
                }

                ParentCanvas.Children.Remove(this);

                ParentCanvas.Children.Remove(PrevInsertThumb);
                ParentCanvas.Children.Remove(NextInsertThumb);
            }
        }

        
        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newLeft = Canvas.GetLeft(this) + e.HorizontalChange;
            double newTop = Canvas.GetTop(this) + e.VerticalChange;

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
            
            int index = GetIndex();
            (Figure.Segments[0] as PolyLineSegment).Points[index] = new Point(newLeft / Scale, newTop / Scale);

            //This is the starting point of the figure
            if (index == 0)
                Figure.StartPoint = new Point(newLeft / Scale, newTop / Scale);
            
            PrevInsertThumb?.UpdatePosition();
            NextInsertThumb?.UpdatePosition();
        }


        private bool rightClicking = false;

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            rightClicking = true;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (rightClicking)
            {
                DeleteVertex();
            }
            rightClicking = false;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.ChangedButton == MouseButton.Middle)
                Console.WriteLine(GetIndex());
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            rightClicking = false;
        }
    }

    public class InsertThumb : Thumb
    {
        public InsertThumb(MapCanvas parent, PathFigure figure, int prevIndex, int nextIndex, double scale, List<MoveThumb> thumbList)
        {
            ParentCanvas = parent;

            ThumbList = thumbList;

            Figure = figure;
            this.scale = scale;
            PointCollection points = (figure.Segments[0] as PolyLineSegment).Points;
            Point prevPosition = points[prevIndex];
            Point nextPosition = points[nextIndex];
            Canvas.SetLeft(this, (prevPosition.X + nextPosition.X) * 0.5 * scale);
            Canvas.SetTop(this, (prevPosition.Y + nextPosition.Y) * 0.5 * scale);
        }


        public MapCanvas ParentCanvas { get; set; }

        public List<MoveThumb> ThumbList { get; set; }
        public MoveThumb PrevThumb { get; set; }
        public MoveThumb NextThumb { get; set; }

        public PathFigure Figure { get; set; }

        private double scale = 1;
        public double Scale
        {
            get { return scale; }
            set
            {
                if (value != scale)
                {
                    Canvas.SetLeft(this, Canvas.GetLeft(this) / scale * value);
                    Canvas.SetTop(this, Canvas.GetTop(this) / scale * value);
                    scale = value;
                }
            }
        }



        public void UpdatePosition()
        {
            PointCollection points = (Figure.Segments[0] as PolyLineSegment).Points;
            Point prevPosition = points[PrevThumb.GetIndex()];
            Point nextPosition = points[NextThumb.GetIndex()];
            Canvas.SetLeft(this, (prevPosition.X + nextPosition.X) * 0.5 * scale);
            Canvas.SetTop(this, (prevPosition.Y + nextPosition.Y) * 0.5 * scale);
        }

        public void InsertVertex(/*bool forceDrag = false*/)
        {
            if (PrevThumb != null && NextThumb != null)
            {
                int prevIndex = PrevThumb.GetIndex();
                int nextIndex = NextThumb.GetIndex();
                int newIndex = nextIndex > prevIndex ? nextIndex : prevIndex + 1; //Correct handling of closed curves
                (Figure.Segments[0] as PolyLineSegment).Points.Insert(newIndex, new Point(Canvas.GetLeft(this) / Scale, Canvas.GetTop(this) / Scale));
                MoveThumb moveThumb = new MoveThumb(ParentCanvas, Figure, newIndex, Scale, ThumbList/*, IsDragging || forceDrag*/);
                ThumbList.Insert(newIndex, moveThumb);
                moveThumb.PrevThumb = PrevThumb;
                moveThumb.NextThumb = NextThumb;
                PrevThumb.NextThumb = moveThumb;
                NextThumb.PrevThumb = moveThumb;

                prevIndex = PrevThumb.GetIndex();
                nextIndex = NextThumb.GetIndex();

                //DependencyObject p = LogicalTreeHelper.GetParent(this);
                //MapCanvas parent = (MapCanvas)VisualTreeHelper.GetParent(this);
                //MapCanvas parent = ((MapCanvas)Parent);

                InsertThumb prevInsertThumb = new InsertThumb(ParentCanvas, Figure, prevIndex, newIndex, Scale, ThumbList);
                prevInsertThumb.PrevThumb = PrevThumb;
                prevInsertThumb.NextThumb = moveThumb;
                moveThumb.PrevInsertThumb = prevInsertThumb;
                PrevThumb.NextInsertThumb = prevInsertThumb;
                ParentCanvas.Children.Add(prevInsertThumb);

                InsertThumb nextInsertThumb = new InsertThumb(ParentCanvas, Figure, newIndex, nextIndex, Scale, ThumbList);
                nextInsertThumb.PrevThumb = moveThumb;
                nextInsertThumb.NextThumb = NextThumb;
                moveThumb.NextInsertThumb = nextInsertThumb;
                NextThumb.PrevInsertThumb = nextInsertThumb;
                ParentCanvas.Children.Add(nextInsertThumb);

                ParentCanvas.Children.Remove(this);
                ParentCanvas.Children.Add(moveThumb);
            }
        }



        private bool leftClicking = false;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            leftClicking = true;
            e.Handled = true;
            Mouse.Capture(this);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            Console.WriteLine("The reoccurrence of this message has long been prohesised. Now it is here. Tell the developers, the time is right, the stars are alligned!");
            if (leftClicking)
            {
                InsertVertex();
            }
            leftClicking = false;
            e.Handled = true;
            Mouse.Capture(null);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (leftClicking)
            {
                InsertVertex(/*true*/);
            }
            leftClicking = false;
            Mouse.Capture(null);
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            InsertVertex();
        }
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
