using System;
using System.Collections;
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
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            map.Position = new PointLatLng(47.473324, 19.0609954);
            map.ShowCenter = false;
            map.Zoom = 18;
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ShowCrosshairEventHandler(object sender, RoutedEventArgs e)
        {
            map.ShowCenter = mapSettings.ShowCrosshair;
            map.InvalidateVisual();
        }
    }
}
