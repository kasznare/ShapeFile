using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PolyEditTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CreateGeometry();
        }


        private void CreateGeometry()
        {
            PathGeometry pathGeometry = new PathGeometry();

            {
                Point[] wpfPoints = new Point[10];
                for (int i = 0; i < wpfPoints.Length; i++)
                {
                    wpfPoints[i] = new Point(150 + 100 * Math.Cos(2 * Math.PI * (i + 1) / 11), 180 + 100 * Math.Sin(2 * Math.PI * (i + 1) / 11));
                }
                Point startPoint = new Point(250, 180);

                PolyLineSegment polyLineSegment = new PolyLineSegment();

                foreach (Point coordinate in wpfPoints)
                    polyLineSegment.Points.Add(coordinate);

                PathFigure figure = new PathFigure
                {
                    StartPoint = startPoint,
                    IsClosed = true,
                    IsFilled = true
                };
                figure.Segments.Add(polyLineSegment);

                pathGeometry.Figures.Add(figure);
            }

            {
                Point[] wpfPoints = new Point[10];
                for (int i = 0; i < wpfPoints.Length; i++)
                {
                    wpfPoints[i] = new Point(150 + 50 * Math.Cos(2 * Math.PI * (i + 1) / 11), 180 - 50 * Math.Sin(2 * Math.PI * (i + 1) / 11));
                }
                Point startPoint = new Point(200, 180);

                PolyLineSegment polyLineSegment = new PolyLineSegment();

                foreach (Point coordinate in wpfPoints)
                    polyLineSegment.Points.Add(coordinate);

                PathFigure figure = new PathFigure
                {
                    StartPoint = startPoint,
                    IsClosed = true,
                    IsFilled = true
                };
                figure.Segments.Add(polyLineSegment);

                pathGeometry.Figures.Add(figure);
            }


            first.Data = pathGeometry;

            PlaceThumbs(pathGeometry);
        }


        private void PlaceThumbs(PathGeometry pathGeometry)
        {
            foreach (PathFigure figure in pathGeometry.Figures)
            {
                {
                    MoveThumb thumb = new MoveThumb();
                    Canvas.SetLeft(thumb, figure.StartPoint.X);
                    Canvas.SetTop(thumb, figure.StartPoint.Y);
                    thumb.Style = FindResource("thumbStyle") as Style;
                    thumb.Template = FindResource("thumbTemplate") as ControlTemplate;
                    canvas.Children.Add(thumb);
                    thumb.DragDelta += Thumb_DragDelta;
                    thumb.Tag = new ThumbTag()
                    {
                        Figure = figure,
                        IsStartPoint = true

                    };
                }
                List<MoveThumb> thumbs = new List<MoveThumb>();
                foreach (Point point in (figure.Segments[0] as PolyLineSegment).Points)
                {
                    MoveThumb thumb = new MoveThumb();
                    Canvas.SetLeft(thumb, point.X);
                    Canvas.SetTop(thumb, point.Y);
                    thumb.Style = FindResource("thumbStyle") as Style;
                    thumb.Template = FindResource("thumbTemplate") as ControlTemplate;
                    thumb.DragDelta += Thumb_DragDelta;
                    thumb.Tag = new ThumbTag()
                    {
                        Figure = figure,
                        IsStartPoint = false,
                        ThumbList = thumbs
                    };

                    canvas.Children.Add(thumb);
                    thumbs.Add(thumb);
                }
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MoveThumb thumb = sender as MoveThumb;

            if (thumb != null)
            {
                double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
                double newTop = Canvas.GetTop(thumb) + e.VerticalChange;

                Canvas.SetLeft(thumb, newLeft);
                Canvas.SetTop(thumb, newTop);

                ThumbTag tag = (ThumbTag)thumb.Tag;
                if (tag.IsStartPoint)
                {
                    tag.Figure.StartPoint = new Point(newLeft, newTop);
                }
                else
                {
                    int index = tag.ThumbList.IndexOf(thumb);
                    (tag.Figure.Segments[0] as PolyLineSegment).Points[index] = new Point(newLeft, newTop);
                }


                //if (index >= 0)
                //{
                //    ((first.Data as PathGeometry).Figures[0].Segments[0] as PolyLineSegment).Points[index] = new Point(newLeft, newTop);
                //    return;
                //}

                //index = startThumbs.IndexOf(thumb);
                //if (index >= 0)
                //{
                //    (first.Data as PathGeometry).Figures[index].StartPoint = new Point(newLeft, newTop);
                //    return;
                //}
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            double value = Double.Parse(tb.Text);
            Console.WriteLine(value);
        }
    }

    public struct ThumbTag
    {
        public PathFigure Figure { get; set; }
        public bool IsStartPoint { get; set; }
        public List<MoveThumb> ThumbList { get; set; }
    }


    public class MoveThumb : Thumb
    {
        public MoveThumb()
        {
            //DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
        }

        //private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        //{
        //    //Control item = this.DataContext as Control;
        //    Control item = this as Control;

        //    if (item != null)
        //    {
        //        double left = Canvas.GetLeft(item);
        //        double top = Canvas.GetTop(item);

        //        Canvas.SetLeft(item, left + e.HorizontalChange);
        //        Canvas.SetTop(item, top + e.VerticalChange);
        //    }

        //    //if (this.DataContext is Point point)
        //    //{
        //    //    point.X += e.HorizontalChange;
        //    //    point.Y += e.VerticalChange;

        //    //    //DataContext = point;



        //    //    double left = point.X;
        //    //    double top = point.Y;

        //    //    Canvas.SetLeft(item, left + e.HorizontalChange);
        //    //    Canvas.SetTop(item, top + e.VerticalChange);
        //    //}
        //}
    }
}
