using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ShapefileEditor
{
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
}
