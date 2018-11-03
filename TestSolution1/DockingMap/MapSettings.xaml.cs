using GMap.NET.MapProviders;
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

namespace DockingMap
{
    /// <summary>
    /// Interaction logic for MapSettings.xaml
    /// </summary>
    public partial class MapSettings : UserControl
    {
        public MapSettings()
        {
            InitializeComponent();
            HideContent();
            this.DataContext = this;
        }

        public static readonly DependencyProperty ProvidersProperty = DependencyProperty.Register("Providers", typeof(List<GMapProvider>), typeof(MapSettings));
        public List<GMapProvider> Providers
        {
            get { return (List<GMapProvider>)GetValue(ProvidersProperty); }
            set { SetValue(ProvidersProperty, value); }
        }

        public static readonly DependencyProperty SelectedProviderProperty = DependencyProperty.Register("SelectedProvider", typeof(GMapProvider), typeof(MapSettings));
        public GMapProvider SelectedProvider
        {
            get { return (GMapProvider)GetValue(SelectedProviderProperty); }
            set { SetValue(SelectedProviderProperty, value); }
        }

        public static readonly DependencyProperty ShowCrosshairProperty = DependencyProperty.Register("ShowCrosshair", typeof(bool), typeof(MapSettings));
        public bool ShowCrosshair
        {
            get { return (bool)GetValue(ShowCrosshairProperty); }
            set
            {
                Console.WriteLine(value);
                SetValue(ShowCrosshairProperty, value); }
        }

        private RoutedEventHandler _showCrosshairChangedEvent;
        public RoutedEventHandler ShowCrosshairChangedEvent
        {
            get { return _showCrosshairChangedEvent; }
            set { _showCrosshairChangedEvent = value; }
        }

        private void ShowContent()
        {
            closedHeader.Visibility = Visibility.Collapsed;
            contentGrid.Visibility = Visibility.Visible;
        }

        private void HideContent()
        {
            contentGrid.Visibility = Visibility.Hidden;
            closedHeader.Visibility = Visibility.Visible;
        }

        private void btnShowContent_Click(object sender, MouseButtonEventArgs e)
        {
            ShowContent();
        }

        private void btnCollapseContent_Click(object sender, MouseButtonEventArgs e)
        {
            HideContent();
        }

        private void cbShowCrosshair_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ShowCrosshairChangedEvent?.Invoke(sender, e);
        }
    }
}
