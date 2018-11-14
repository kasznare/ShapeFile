using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using GeoAPI.Geometries;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Microsoft.Win32;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Windows.Media;

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //MapCanvas canvas;
        public ObservableCollection<Layer> Layers { get; private set; }
        
        public ShapefileShape SelectedShape
        {
            get { return (ShapefileShape)GetValue(SelectedShapeProperty); }
            set { SetValue(SelectedShapeProperty, value); }
        }
        public static readonly DependencyProperty SelectedShapeProperty =
            DependencyProperty.Register("SelectedShape", typeof(ShapefileShape), typeof(MainWindow));



        public MainWindow()
        {
            InitializeComponent();

            Themes.Load("VS_blue");

            List<GMapProvider> choosableMapProviders = new List<GMapProvider>();
            choosableMapProviders.Add(GMapProviders.OpenStreetMap);
            choosableMapProviders.Add(GMapProviders.GoogleMap);
            choosableMapProviders.Add(GMapProviders.GoogleHybridMap);
            choosableMapProviders.Add(GMapProviders.GoogleSatelliteMap);
            choosableMapProviders.Add(GMapProviders.GoogleTerrainMap);
            mapSettings.Providers = choosableMapProviders;

            map.EmptyMapBackground = new SolidColorBrush(Colors.Black);
            map.DragButton = MouseButton.Left;
            //map.Position = new PointLatLng(47.473324, 19.0609954);
            map.Position = new PointLatLng(0, 0);
            map.ShowCenter = false;
            map.Zoom = 18;

            //canvas = new MapCanvas(map);
            //map.Markers.Add(new GMapMarker(map.MapProvider.Projection.Bounds.LocationTopLeft)
            //{
            //    Shape = canvas,
            //    Offset = new System.Windows.Point(0, 0)
            //});

            map.OnMapZoomChanged += Map_OnMapZoomChanged;
            
            //textBoxMessages.AppendText("Projection bounds: " + map.MapProvider.Projection.Bounds.ToString() + "\n");

            Layers = new ObservableCollection<Layer>();

            DataContext = this;

            this.Closed += delegate (Object o, EventArgs e) { consoleRedirectWriter.Release(); };  //sets releases console when window closes.
        }

        ConsoleRedirectWriter consoleRedirectWriter = new ConsoleRedirectWriter();

        private void Map_OnMapZoomChanged()
        {
            //textBoxMessages.AppendText("Projection bounds: " + map.MapProvider.Projection.GetTileMatrixSizePixel((int)map.Zoom) + "\n");
            //textBoxMessages.AppendText("Bottom right: " + map.MapProvider.Projection.FromPixelToTileXY(map.MapProvider.Projection.FromLatLngToPixel(map.MapProvider.Projection.Bounds.LocationRightBottom, (int)map.Zoom)).ToString() + "\n");
            //Console.WriteLine("Bottom right: " + map.MapProvider.Projection.FromPixelToTileXY(map.MapProvider.Projection.FromLatLngToPixel(map.MapProvider.Projection.Bounds.LocationRightBottom, (int)map.Zoom)).ToString());
        }

        private void ShowCrosshairEventHandler(object sender, RoutedEventArgs e)
        {
            map.ShowCenter = mapSettings.ShowCrosshair;
            map.InvalidateVisual();
        }

        #region Commands

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        private void OpenCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            openFileDialog.Filter = "Shapefiles (*.shp)|*.shp";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Layer layer = new Layer(openFileDialog.SafeFileName);

                    //ShapefileReader reader = new ShapefileReader(openFileDialog.FileName);
                    //GMapPathGeometryWriter wpfPathGeometryWriter = new GMapPathGeometryWriter();
                    //foreach (IGeometry geom in reader)
                    //{
                    //    GMapNTSGeometryMarker poly = new GMapNTSGeometryMarker(wpfPathGeometryWriter.ToShape(geom) as PathGeometry);
                    //    map.Markers.Add(poly);
                    //    map.RegenerateShape(poly);
                    //}

                    using (var dataReader = new ShapefileDataReader(openFileDialog.FileName, new GeometryFactory()))
                    {
                        layer.SetDbaseHeader(dataReader.DbaseHeader);
                        //foreach (var entry in dataReader)
                        //{
                        //    Console.WriteLine(entry);
                        //    //Console.WriteLine(entry.ColumnValues[2]);
                        //}
                        int length = dataReader.DbaseHeader.NumFields;
                        while (dataReader.Read())
                        {
                            //GMapNTSGeometryMarker marker = new GMapNTSGeometryMarker(dataReader.Geometry);
                            //map.Markers.Add(marker);

                            //CanvasShape canvasShape = new CanvasShape(map, dataReader.Geometry);
                            //canvasShape.Fill = new SolidColorBrush(Colors.Red);
                            //canvas.Children.Add(canvasShape);

                            ShapefileShape shape = new ShapefileShape("shape", dataReader.Geometry);
                            for (int i = 0; i < length; i++)
                            {
                                shape.Attributes.Add(new ShapefileAttributeEntry(layer.Attributes[i], dataReader.GetValue(i + 1)));
                            }


                            layer.Shapes.Add(shape);
                            //dataReader[]
                        }

                        Layers.Insert(0, layer);
                        MapCanvas canvas = new MapCanvas(map, layer);
                        map.Markers.Add(canvas.Marker);
                        SelectedShape = Layers[0].Shapes[0];
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        #endregion Commands

        private void textBoxMessages_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxMessages.ScrollToEnd();
        }

        String LastConsoleString;
        private void textBoxMessages_Initialized(object sender, EventArgs e)
        {
            // Use this for thread safe objects or UIElements in a single thread program
            consoleRedirectWriter.OnWrite += delegate (string value) { LastConsoleString = value; };

            // Multithread operation - Use the dispatcher to write to WPF UIElements if there is more than 1 thread.
            //consoleRedirectWriter.OnWrite += Dispatcher.BeginInvoke(
            //    System.Windows.Threading.DispatcherPriority.Normal,
            //    (Action<string>)delegate (string value) 
            //    {
            //        textBoxMessages.AppendText(value);
            //        textBoxMessages.ScrollToEnd();
            //    });
            consoleRedirectWriter.OnWrite += delegate (string value)
                {
                    textBoxMessages.AppendText(value);
                    textBoxMessages.ScrollToEnd();
                };
        }
    }

    public class Layer : DependencyObject
    {
        public Layer(string name) : base()
        {
            Name = name;
            Attributes = new ObservableCollection<DbaseFieldDescriptor>();
            Shapes = new ObservableCollection<ShapefileShape>();
        }



        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(Layer), new PropertyMetadata("New Shapefile"));
        
        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(Layer));
        
        public bool IsLayerVisible
        {
            get { return (bool)GetValue(IsLayerVisibleProperty); }
            set { SetValue(IsLayerVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsLayerVisibleProperty = DependencyProperty.Register("IsLayerVisible", typeof(bool), typeof(Layer), new PropertyMetadata(true));

        public ObservableCollection<DbaseFieldDescriptor> Attributes { get; private set; }

        public ObservableCollection<ShapefileShape> Shapes { get; private set; }

        private DbaseFileHeader dbaseHeader;
        public void SetDbaseHeader(DbaseFileHeader dbaseFileHeader)
        {
            dbaseHeader = dbaseFileHeader;
            for (int i = 0; i < dbaseHeader.NumFields; i++)
            {
                Attributes.Add(dbaseHeader.Fields[i]);
            }
        }

        public IList Children
        {
            get
            {
                return new[]
                {
                    new ShapefileCollectionContainer("Attributes", Attributes ),
                    new ShapefileCollectionContainer("Shapes", Shapes )
                };
            }
        }
        
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(Layer), new PropertyMetadata(new SolidColorBrush(Colors.AliceBlue)));
        
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(Layer), new PropertyMetadata(new SolidColorBrush(Colors.Navy)));
        
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(Layer), new PropertyMetadata(2.0));
    }

    public class ShapefileCollectionContainer : CollectionContainer
    {
        public ShapefileCollectionContainer(string name, IEnumerable coll)
        {
            Name = name;
            Collection = coll;
        }

        public string Name { get; private set; }
    }

    //public class ShapefileAttributeKey
    //{
    //    private DbaseFieldDescriptor dbaseFieldDescriptor;

    //    public ShapefileAttributeKey(DbaseFieldDescriptor dbaseFieldDescriptor)
    //    {
    //        this.dbaseFieldDescriptor = dbaseFieldDescriptor;
    //        Name = dbaseFieldDescriptor.Name;
    //    }

    //    public string Name { get; private set; }
    //}

    public class ShapefileShape
    {
        public ShapefileShape(string name, IGeometry geometry)
        {
            Name = name;
            Geometry = geometry;
            Attributes = new ObservableCollection<ShapefileAttributeEntry>();
        }

        public string Name { get; private set; }
        public IGeometry Geometry { get; private set; }

        public ObservableCollection<ShapefileAttributeEntry> Attributes { get; private set; }
    }

    public class ShapefileAttributeEntry
    {
        public DbaseFieldDescriptor FieldDescriptor { get; }
        public object Value { get; set; }
        
        public ShapefileAttributeEntry(DbaseFieldDescriptor dbaseFieldDescriptor, object value)
        {
            FieldDescriptor = dbaseFieldDescriptor;
            Value = value;
        }
    }

    public class SolidColorBrushToColorValueConverter : IValueConverter
    {

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
                return null;
            }
            // For a more sophisticated converter, check also the targetType and react accordingly..
            if (value is Color)
            {
                Color color = (Color)value;
                return new SolidColorBrush(color);
            }
            // You can support here more source types if you wish
            // For the example I throw an exception

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If necessary, here you can convert back. Check if which brush it is (if its one),
            // get its Color-value and return it.

            if (null == value)
            {
                return null;
            }
            // For a more sophisticated converter, check also the targetType and react accordingly..
            if (value is SolidColorBrush)
            {
                SolidColorBrush brush = (SolidColorBrush)value;
                return brush.Color;
            }

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }
    }
}
