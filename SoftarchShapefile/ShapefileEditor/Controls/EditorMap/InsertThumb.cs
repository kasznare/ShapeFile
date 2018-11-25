using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ShapefileEditor
{
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
            if (leftClicking)
                InsertVertex();
            leftClicking = false;
            e.Handled = true;
            Mouse.Capture(null);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (leftClicking)
                InsertVertex(/*true*/);
            leftClicking = false;
            Mouse.Capture(null);
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            InsertVertex();
        }
    }
}
