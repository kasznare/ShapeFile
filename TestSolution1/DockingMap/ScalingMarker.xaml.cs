using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for ScalingMarker.xaml
    /// </summary>
    public partial class ScalingMarker : UserControl
    {
        public ScalingMarker(Map map)
        {
            InitializeComponent();

            Binding binding = new Binding("Zoom");
            binding.Source = map;
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Converter = map.ZoomConverter;
            SetBinding(ZoomProperty, binding);
        }

        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register("Child", typeof(FrameworkElement), typeof(ScalingMarker));
        public FrameworkElement Child
        {
            get { return (FrameworkElement)GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(int), typeof(MapCanvas));
        public int Zoom
        {
            get { return (int)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }
    }
}
