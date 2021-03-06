﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.MapProviders;
using Microsoft.Win32;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace ShapefileEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static PointLatLng BMECoordinates = new PointLatLng(47.473324, 19.0609954);
        
        public MainWindow()
        {
            InitializeComponent();

            Themes.Load("StudioBlue");

            openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName,
                Filter = "Shapefiles (*.shp)|*.shp"
            };
            saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName,
                Filter = "Shapefiles (*.shp)|*.shp"
            };

            List<GMapProvider> choosableMapProviders = new List<GMapProvider>
            {
                GMapProviders.OpenStreetMap,
                GMapProviders.GoogleMap,
                GMapProviders.GoogleHybridMap,
                GMapProviders.GoogleSatelliteMap,
                GMapProviders.GoogleTerrainMap
            };
            mapSettings.Providers = choosableMapProviders;

            map.EmptyMapBackground = new SolidColorBrush(Colors.Black);
            map.DragButton = MouseButton.Left;
            map.ShowCenter = false;
            //map.Position = BMECoordinates;
            map.Position = new PointLatLng(0, 0);
            //map.Zoom = 18;
            map.Zoom = 2;

            Layers = new ObservableCollection<Layer>();

            DataContext = this;

            mapCanvas = new MapCanvas(map)
            {
                Layers = Layers
            };
            map.Markers.Add(mapCanvas.Marker);

            Closed += delegate (object o, EventArgs e) { consoleRedirectWriter.Release(); };
        }
        
        MapCanvas mapCanvas;
        public ObservableCollection<Layer> Layers { get; private set; }
        OpenFileDialog openFileDialog;
        SaveFileDialog saveFileDialog;

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
                    layer.Name = Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
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
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(layerToSave.FileName);
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

                    ShapefileDataWriter writer = new ShapefileDataWriter(name, GeometryFactory.Default)
                    {
                        Header = layerToSave.Header
                    };
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
                    if (MessageBox.Show("Are you sure you want to delete this layer?", "Delete Layer", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        Layers.Remove((Layer)CollectionViewSource.GetDefaultView(Layers).CurrentItem);
                    break;
                case "field":
                    currentLayer = (Layer)CollectionViewSource.GetDefaultView(Layers)?.CurrentItem;
                    int currentIndex = CollectionViewSource.GetDefaultView(currentLayer.Attributes).CurrentPosition;
                    DbaseFieldDescriptor descriptor = currentLayer.Attributes[currentIndex];
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

        #region MoveDown command
        private void MoveDownCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            switch ((string)e.Parameter)
            {
                case "layer":
                    e.CanExecute = CollectionViewSource.GetDefaultView(Layers).CurrentPosition < Layers.Count - 1;
                    break;
                default:
                    e.CanExecute = false;
                    break;
            }
        }
        private void MoveDownCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch ((string)e.Parameter)
            {
                case "layer":
                    int currentPosition = CollectionViewSource.GetDefaultView(Layers).CurrentPosition;
                    if (currentPosition < Layers.Count - 1)
                        Layers.Move(currentPosition, currentPosition + 1);
                    break;
                default:
                    break;
            }
        }
        #endregion MoveDown command

        #region MoveUp command
        private void MoveUpCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            switch ((string)e.Parameter)
            {
                case "layer":
                    e.CanExecute = CollectionViewSource.GetDefaultView(Layers).CurrentPosition > 0;
                break;
                default:
                        e.CanExecute = false;
                break;
            }
        }
        private void MoveUpCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch ((string)e.Parameter)
            {
                case "layer":
                    int currentPosition = CollectionViewSource.GetDefaultView(Layers).CurrentPosition;
                    if (currentPosition > 0)
                        Layers.Move(currentPosition, currentPosition - 1);
                    break;
                default:
                    break;
            }
        }
        #endregion MoveUp command

        #endregion Commands
        
        #region Console output redirection

        ConsoleRedirectWriter consoleRedirectWriter = new ConsoleRedirectWriter();

        private void textBoxMessages_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxMessages.ScrollToEnd();
        }

        string LastConsoleString;
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

        #endregion Console output redirection
    }
}
