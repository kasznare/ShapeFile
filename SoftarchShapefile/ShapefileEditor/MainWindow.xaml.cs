using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

            Themes.Load("StudioBlue");
            
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

        #region New command
        private void NewCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            switch ((string)e.Parameter)
            {
                case "layer":
                    e.CanExecute = true;
                    break;
                case "field":
                    e.CanExecute = CollectionViewSource.GetDefaultView(Layers)?.CurrentItem != null;
                    break;
                case "shape":
                    e.CanExecute = CollectionViewSource.GetDefaultView(Layers)?.CurrentItem != null;
                    break;
                default:
                    e.CanExecute = false;
                    break;
            }
        }
        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Layer currentLayer;
            switch ((string)e.Parameter)
            {
                case "layer":
                    NewLayerDialog newLayerDialog = new NewLayerDialog();
                    if (newLayerDialog.ShowDialog() == true)
                    {
                        Layer newLayer = new Layer();
                        DbaseFileReader reader = new DbaseFileReader(
                            new NetTopologySuite.IO.Streams.ByteStreamProvider(
                                NetTopologySuite.IO.Streams.StreamTypes.Data,
                                new MemoryStream(Properties.Resources.EmptyDbaseHeader1)));
                        newLayer.Header = reader.GetHeader();
                        newLayer.Name = newLayerDialog.LayerName;
                        newLayer.ShapeType = newLayerDialog.ShapeType;
                        Layers.Add(newLayer);
                    }
                    break;
                case "field":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem;
                    currentLayer.Header.AddColumn(newFieldName.Text, ((string)((ComboBoxItem)newFieldType.SelectedItem).Tag)[0], newFieldLength.Value ?? 0, newFieldDecimals.Value ?? 0);
                    DbaseFieldDescriptor descriptor = currentLayer.Header.Fields.Last();
                    currentLayer.Attributes.Add(descriptor);
                    object newValue = null;
                    var @switch = new Dictionary<Type, Action> {
                        { typeof(Int32), () => newValue = 0 },
                        { typeof(Double), () => newValue = 0.0 },
                        { typeof(String), () => newValue = "" },
                        { typeof(DateTime), () => newValue = DateTime.UtcNow },
                        { typeof(Boolean), () => newValue = false },
                    };
                    @switch[descriptor.Type]();
                    foreach (ShapefileShape shape in currentLayer.Shapes)
                        shape.Attributes.Add(new ShapefileAttributeEntry(descriptor, newValue));
                    break;
                case "shape":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem;
                    ShapefileShape newShape = new ShapefileShape(currentLayer.ShapeType);
                    newShape.CreateAttributes(currentLayer.Attributes);
                    currentLayer.Shapes.Add(newShape);
                    break;
                default:
                    break;
            }
        }
        #endregion New command

        #region Open command
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
                    Layer layer = new Layer();
                    layer.Name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                    layer.FileName = openFileDialog.FileName;
                    using (var dataReader = new ShapefileDataReader(openFileDialog.FileName, new GeometryFactory()))
                    {
                        layer.Header = dataReader.DbaseHeader;
                        layer.ShapeGeometryType = dataReader.ShapeHeader.ShapeType;
                        int numRecords = dataReader.DbaseHeader.NumRecords;
                        int length = dataReader.DbaseHeader.NumFields;
                        for (int j = 0; j < numRecords; j++)
                        {
                            dataReader.Read();
                            ShapefileShape shape = new ShapefileShape(dataReader.Geometry);
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
        #endregion Open command

        #region Save command
        private void SaveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CollectionViewSource.GetDefaultView(Layers)?.CurrentItem != null;
        }
        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Layer layerToSave = CollectionViewSource.GetDefaultView(Layers).CurrentItem as Layer;
            if (layerToSave.FileName != "")
            {
                saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(layerToSave.FileName);
                saveFileDialog.InitialDirectory = layerToSave.FileName;

            }
            if (saveFileDialog.ShowDialog() == true)
            {
                string name = saveFileDialog.FileName;
                try
                {
                    mapCanvas.CommitAll();

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
                    ShapefileDataWriter writer = new ShapefileDataWriter(baseFileName, GeometryFactory.Default);
                    writer.Header = layerToSave.Header;
                    writer.Write(features);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        #endregion Save command

        #region Delete command
        private void DeleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            Layer currentLayer;
            switch ((string)e.Parameter)
            {
                case "layer":
                    e.CanExecute = CollectionViewSource.GetDefaultView(Layers).CurrentItem != null;
                    break;
                case "field":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers)?.CurrentItem;
                    e.CanExecute = currentLayer != null && CollectionViewSource.GetDefaultView(currentLayer.Attributes).CurrentItem != null;
                    break;
                case "shape":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers)?.CurrentItem;
                    e.CanExecute = currentLayer != null && CollectionViewSource.GetDefaultView(currentLayer.Shapes).CurrentItem != null;
                    break;
                default:
                    e.CanExecute = false;
                    break;
            }
        }
        private void DeleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Layer currentLayer;
            switch ((string)e.Parameter)
            {
                case "layer":
                    MessageBoxResult dialogResult = MessageBox.Show("Are you sure you want to delete this layer?", "Delete Layer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        Layers.Remove((Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem);
                    }
                    break;
                case "field":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers)?.CurrentItem;
                    int currentIndex = CollectionViewSource.GetDefaultView(currentLayer.Attributes).CurrentPosition;
                    DbaseFieldDescriptor descriptor = currentLayer.Attributes[currentIndex];
                    Console.WriteLine(descriptor.Name);
                    currentLayer.Header.RemoveColumn(descriptor.Name);
                    currentLayer.Attributes.RemoveAt(currentIndex);
                    foreach (ShapefileShape shape in currentLayer.Shapes)
                        shape.Attributes.RemoveAt(currentIndex);
                    break;
                case "shape":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers)?.CurrentItem;
                    var shapes = CollectionViewSource.GetDefaultView(currentLayer.Shapes);
                    currentLayer.Shapes.Remove((ShapefileShape)shapes.CurrentItem);
                    shapes.MoveCurrentTo(null);
                    break;
                default:
                    break;
            }
        }
        #endregion Delete command

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



    public class SolidColorBrushToColorValueConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
                return null;
            }
            if (value is Color)
            {
                Color color = (Color)value;
                return new SolidColorBrush(color);
            }

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is SolidColorBrush)
            {
                SolidColorBrush brush = (SolidColorBrush)value;
                return brush.Color;
            }

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }
    }

    public class DecimalToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            return (decimal)(double)value;
            if (value is int)
                return (decimal)(int)value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is decimal)
            {
                //if (targetType == typeof(Int32))
                //    return (int)(decimal)value;
                //if (targetType == typeof(Double))
                    return (double)(decimal)value;
                //return value;
            }
            return value;
        }
    }

    public class DecimalCountToIncrementValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            if (value is int)
                return (decimal)Math.Pow(0.1, (int)value);

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            if (value is decimal)
                return -Math.Log10((double)value);

            Type type = value.GetType();
            throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
        }
    }

    public class FieldMinMaxValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Decimal))
            {
                if (value == null || parameter == null)
                    return null;
                if (value is DbaseFieldDescriptor)
                    if (parameter is string)
                    {
                        DbaseFieldDescriptor descriptor = (DbaseFieldDescriptor)value;
                        string param = ((string)parameter).ToLower();
                        if (param == "min")
                            return (decimal)((1 - Math.Pow(10, descriptor.Length - 1)) * Math.Pow(0.1, descriptor.DecimalCount));
                        else if (param == "max")
                            return (decimal)((Math.Pow(10, descriptor.Length) - 1) * Math.Pow(0.1, descriptor.DecimalCount));
                    }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
