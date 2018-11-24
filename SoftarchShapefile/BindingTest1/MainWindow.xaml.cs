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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BindingTest1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            tb6.Text = "Programozott, OneWayToSource";
            Binding binding = new Binding("Text");
            binding.Source = tb6;
            binding.Mode = BindingMode.OneWayToSource;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            tb5.SetBinding(TextBox.TextProperty, binding);

            
            binding = new Binding("TextHidden");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            tb7.SetBinding(TextBox.TextProperty, binding);
            
            binding = new Binding("TextHidden");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            tb8.SetBinding(TextBox.TextProperty, binding);

            
        }

        DependencyProperty TextHiddenProperty = DependencyProperty.Register("TextHidden", typeof(string), typeof(MainWindow));
        public string TextHidden
        {
            get { return (string)GetValue(TextHiddenProperty); }
            set { SetValue(TextHiddenProperty, value); }
        }

        public static readonly DependencyProperty ShowTFProperty = DependencyProperty.Register("ShowTF", typeof(bool), typeof(MainWindow));
        public bool ShowTF
        {
            get { return (bool)GetValue(ShowTFProperty); }
            set
            {
                Console.WriteLine(value);
                SetValue(ShowTFProperty, value);
            }
        }
    }
}
