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
        private static PointLatLng BMECoordinates = new PointLatLng(47.473324, 19.0609954);
        
        OpenFileDialog openFileDialog;
        SaveFileDialog saveFileDialog;
        
        //MapCanvas canvas;
        public ObservableCollection<Layer> Layers { get; private set; }



        public MainWindow()
        {
            InitializeComponent();

            Themes.Load("VS_blue");
            
            openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            openFileDialog.Filter = "Shapefiles (*.shp)|*.shp";

            saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = System.IO.Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            saveFileDialog.Filter = "Shapefiles (*.shp)|*.shp";

            List<GMapProvider> choosableMapProviders = new List<GMapProvider>();
            choosableMapProviders.Add(GMapProviders.OpenStreetMap);
            choosableMapProviders.Add(GMapProviders.GoogleMap);
            choosableMapProviders.Add(GMapProviders.GoogleHybridMap);
            choosableMapProviders.Add(GMapProviders.GoogleSatelliteMap);
            choosableMapProviders.Add(GMapProviders.GoogleTerrainMap);
            mapSettings.Providers = choosableMapProviders;

            map.EmptyMapBackground = new SolidColorBrush(Colors.Black);
            map.DragButton = MouseButton.Left;
            //map.Position = BMECoordinates;
            map.Position = new PointLatLng(0, 0);
            map.ShowCenter = false;
            //map.Zoom = 18;
            map.Zoom = 2;

            Layers = new ObservableCollection<Layer>();

            DataContext = this;
            
            MapCanvas canvas = new MapCanvas(map);
            canvas.Layers = Layers;
            map.Markers.Add(canvas.Marker);

            Closed += delegate (object o, EventArgs e) { consoleRedirectWriter.Release(); };  //sets releases console when window closes.
        }

        ConsoleRedirectWriter consoleRedirectWriter = new ConsoleRedirectWriter();

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
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Layer layer = new Layer(openFileDialog.SafeFileName);
                    using (var dataReader = new ShapefileDataReader(openFileDialog.FileName, new GeometryFactory()))
                    {
                        layer.SetDbaseHeader(dataReader.DbaseHeader);
                        int length = dataReader.DbaseHeader.NumFields;
                        while (dataReader.Read())
                        {
                            ShapefileShape shape = new ShapefileShape("shape", dataReader.Geometry);
                            for (int i = 0; i < length; i++)
                            {
                                shape.Attributes.Add(new ShapefileAttributeEntry(layer.Attributes[i], dataReader.GetValue(i + 1)));
                            }
                            layer.Shapes.Add(shape);
                        }
                        Layers.Add(layer);
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

        private void btnMoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            int currentPosition = CollectionViewSource.GetDefaultView(Layers).CurrentPosition;
            if (currentPosition > 0)
                Layers.Move(currentPosition, currentPosition - 1);
        }

        private void btnMoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            int currentPosition = CollectionViewSource.GetDefaultView(Layers).CurrentPosition;
            if (currentPosition < Layers.Count - 1)
                Layers.Move(currentPosition, currentPosition + 1);
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
        
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(Layer), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(127, 240, 248, 255))));
        
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
