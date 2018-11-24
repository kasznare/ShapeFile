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
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Windows.Media;

namespace ShapefileEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static PointLatLng BMECoordinates = new PointLatLng(47.473324, 19.0609954);
        
        OpenFileDialog openFileDialog;
        SaveFileDialog saveFileDialog;
        
        MapCanvas mapCanvas;
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
            
            mapCanvas = new MapCanvas(map);
            mapCanvas.Layers = Layers;
            map.Markers.Add(mapCanvas.Marker);

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

        private void SaveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CollectionViewSource.GetDefaultView(Layers)?.CurrentItem != null;
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (saveFileDialog.ShowDialog() == true)
            {
                string name = saveFileDialog.FileName;
                try
                {
                    mapCanvas.CommitAll();

                    Layer layerToSave = CollectionViewSource.GetDefaultView(Layers).CurrentItem as Layer;
                    List<Feature> features = new List<Feature>();

                    foreach (ShapefileShape item in layerToSave.Shapes)
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        foreach (ShapefileAttributeEntry items in item.Attributes)
                        {
                            dictionary.Add(items.FieldDescriptor.Name, items.Value);
                        }
                        features.Add(new Feature(item.Geometry, new AttributesTable(dictionary)));
                    }

                    string baseFileName = name;

                    //string shpFilePath = baseFileName + ".shp";string dbfFilePath = baseFileName + ".dbf";string shxFilePath = baseFileName + ".shx";
                    //var reg = new ShapefileStreamProviderRegistry(shapeStream: new FileStreamProvider(StreamTypes.Shape, shpFilePath),dataStream: new FileStreamProvider(StreamTypes.Data, dbfFilePath),
                    //    indexStream: new FileStreamProvider(StreamTypes.Index, shxFilePath),validateShapeProvider: true,validateDataProvider: true,validateIndexProvider: true);
                    //ShapefileDataWriter wr = new ShapefileDataWriter(reg, GeometryFactory.Default, CodePagesEncodingProvider.Instance.GetEncoding(1252));
                    ShapefileDataWriter wr = new ShapefileDataWriter(baseFileName, GeometryFactory.Default);
                    wr.Header = layerToSave.GetDbaseFileHeader();
                    wr.Write(features);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private void DeleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if ((string)e.Parameter == "layer")
            {
                e.CanExecute = CollectionViewSource.GetDefaultView(Layers).CurrentItem != null;
            }
        }

        private void DeleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if ((string)e.Parameter == "layer")
            {
                MessageBoxResult dialogResult = MessageBox.Show("Are you sure you want to delete this layer?", "Delete Layer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    Layers.Remove((Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem);
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

        private void btnDeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dialogResult = MessageBox.Show("Do you sure you want to delete this layer?", "Delete Layer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dialogResult == MessageBoxResult.Yes)
            {
                Layers.Remove((Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem);
            }
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
        public DbaseFileHeader DbaseHeader { get; }
        public DbaseFileHeader GetDbaseFileHeader()
        {
            return dbaseHeader;
        }
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





    public class ShapefileShape : DependencyObject
    {
        public ShapefileShape(string name, IGeometry geometry)
        {
            Name = name;
            Geometry = geometry;
            Attributes = new ObservableCollection<ShapefileAttributeEntry>();
        }

        public string Name { get; private set; }

        public IGeometry Geometry
        {
            get { return (IGeometry)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(IGeometry), typeof(ShapefileShape), new PropertyMetadata(null));
        
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
