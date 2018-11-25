using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ShapefileEditor
{
    /// <summary>
    /// Interaction logic for NewLayerWindow.xaml
    /// </summary>
    public partial class NewLayerDialog : Window
    {
        public NewLayerDialog()
        {
            InitializeComponent();

            DataContext = this;

            newLayerType.ItemsSource = Enum.GetValues(typeof(AllowedShapeType)).Cast<AllowedShapeType>();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        
        public string LayerName
        {
            get { return (string)GetValue(LayerNameProperty); }
            set { SetValue(LayerNameProperty, value); }
        }
        public static readonly DependencyProperty LayerNameProperty = DependencyProperty.Register("LayerName", typeof(string), typeof(NewLayerDialog), new PropertyMetadata("Layer"));
        
        public AllowedShapeType ShapeType
        {
            get { return (AllowedShapeType)GetValue(ShapeTypeProperty); }
            set { SetValue(ShapeTypeProperty, value); }
        }
        public static readonly DependencyProperty ShapeTypeProperty = DependencyProperty.Register("ShapeType", typeof(AllowedShapeType), typeof(NewLayerDialog), new PropertyMetadata(AllowedShapeType.Polygon));

    }
}
