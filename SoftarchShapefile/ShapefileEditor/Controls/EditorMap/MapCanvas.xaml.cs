using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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
}
