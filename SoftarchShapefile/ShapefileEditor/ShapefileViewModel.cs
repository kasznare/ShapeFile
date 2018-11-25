using GeoAPI.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShapefileEditor
{
    public enum AllowedShapeType { Point, Line, Polygon }

    public class Layer : DependencyObject
    {
        public Layer() : base()
        {
            Attributes = new ObservableCollection<DbaseFieldDescriptor>();
            Shapes = new ObservableCollection<ShapefileShape>();
            Shapes.CollectionChanged += Shapes_CollectionChanged;
        }

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(Layer), new PropertyMetadata("NewShapefile"));

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(Layer), new PropertyMetadata(""));

        public bool IsLayerVisible
        {
            get { return (bool)GetValue(IsLayerVisibleProperty); }
            set { SetValue(IsLayerVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsLayerVisibleProperty = DependencyProperty.Register("IsLayerVisible", typeof(bool), typeof(Layer), new PropertyMetadata(true));

        public ObservableCollection<DbaseFieldDescriptor> Attributes { get; private set; }

        public ObservableCollection<ShapefileShape> Shapes { get; private set; }

        private void Shapes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Header != null)
            {
                if ((e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && Shapes.Count - Header.NumRecords == 1)
                    || (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && Header.NumRecords - Shapes.Count == 1))
                    Header.NumRecords = Shapes.Count;
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    Header.NumFields = 0;
            }
        }

        public DbaseFileHeader Header
        {
            get { return (DbaseFileHeader)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(DbaseFileHeader), typeof(Layer), new PropertyMetadata(new DbaseFileHeader(), new PropertyChangedCallback(HeaderChanged)));

        private static void HeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Layer layer = (Layer)d;
            DbaseFileHeader header = (DbaseFileHeader)e.NewValue;
            for (int i = 0; i < header.NumFields; i++)
            {
                layer.Attributes.Add(header.Fields[i]);
            }
        }

        public ShapeGeometryType ShapeGeometryType
        {
            get { return (ShapeGeometryType)GetValue(ShapeGeometryTypeProperty); }
            internal set { SetValue(ShapeGeometryTypeProperty, value); }
        }
        public static readonly DependencyProperty ShapeGeometryTypeProperty = DependencyProperty.Register("ShapeGeometryType", typeof(ShapeGeometryType), typeof(Layer), new PropertyMetadata(null));

        public AllowedShapeType ShapeType
        {
            get { return (AllowedShapeType)GetValue(ShapeTypeProperty); }
            internal set { SetValue(ShapeTypeProperty, value); }
        }
        public static readonly DependencyProperty ShapeTypeProperty = DependencyProperty.Register("ShapeType", typeof(AllowedShapeType), typeof(Layer), new PropertyMetadata(AllowedShapeType.Polygon, new PropertyChangedCallback(ShapeTypeChanged)));

        private static void ShapeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Layer layer = (Layer)d;
            switch (e.NewValue)
            {
                case AllowedShapeType.Point:
                    layer.ShapeGeometryType = ShapeGeometryType.PointM;
                    break;
                case AllowedShapeType.Line:
                    layer.ShapeGeometryType = ShapeGeometryType.LineStringM;
                    break;
                case AllowedShapeType.Polygon:
                    layer.ShapeGeometryType = ShapeGeometryType.PolygonM;
                    break;
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
        public ShapefileShape(AllowedShapeType shapeType)
        {
            ShapeType = shapeType;
            Attributes = new ObservableCollection<ShapefileAttributeEntry>();
        }

        public ShapefileShape(IGeometry geometry)
        {
            Geometry = geometry;
            Attributes = new ObservableCollection<ShapefileAttributeEntry>();
        }

        public string Name { get; private set; } = "Shape";

        public AllowedShapeType ShapeType { get; private set; }
        
        public IGeometry Geometry
        {
            get { return (IGeometry)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(IGeometry), typeof(ShapefileShape), new PropertyMetadata(null));
        
        public ObservableCollection<ShapefileAttributeEntry> Attributes { get; private set; }

        public void CreateAttributes(Collection<DbaseFieldDescriptor> descriptors)
        {
            foreach (DbaseFieldDescriptor descriptor in descriptors)
            {
                object newValue = null;
                var @switch = new Dictionary<Type, Action> {
                        { typeof(Int32), () => newValue = 0 },
                        { typeof(Double), () => newValue = 0.0 },
                        { typeof(String), () => newValue = "" },
                        { typeof(DateTime), () => newValue = DateTime.UtcNow },
                        { typeof(Boolean), () => newValue = false },
                    };
                @switch[descriptor.Type]();
                Attributes.Add(new ShapefileAttributeEntry(descriptor, newValue));
            }
        }
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
}
