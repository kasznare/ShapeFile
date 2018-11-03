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
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace MapTest1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // config map
            Map.MapProvider = GMapProviders.GoogleSatelliteMap;//.OpenStreetMap;
            Map.Position = new PointLatLng(47, 19);

            // map events
            //Map.OnPositionChanged += new PositionChanged(Map_OnCurrentPositionChanged);
            //Map.OnTileLoadComplete += new TileLoadComplete(Map_OnTileLoadComplete);
            //Map.OnTileLoadStart += new TileLoadStart(Map_OnTileLoadStart);
            //Map.OnMapTypeChanged += new MapTypeChanged(Map_OnMapTypeChanged);
            //Map.MouseMove += new System.Windows.Input.MouseEventHandler(Map_MouseMove);
            //Map.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(Map_MouseLeftButtonDown);
            //Map.MouseEnter += new MouseEventHandler(Map_MouseEnter);
        }
    }
}
