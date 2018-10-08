using Exam_stadium_threads.StadiumRoot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Exam_stadium_threads
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Stadium CurrStadium { get; set; }
        public Thread fanGenerator { get; set; }


        public static readonly DependencyProperty GenerateFunSleepProperty;
        public int GenerateFunSleep
        {
            get { return (int)GetValue(GenerateFunSleepProperty); }
            set { SetValue(GenerateFunSleepProperty, value); }
        } 
        private Dictionary<int, ushort> _sectorPlases;
        static MainWindow()
        {
            GenerateFunSleepProperty = DependencyProperty.Register("GenerateFunSleep", typeof(int), typeof(MainWindow));
        }
        public MainWindow()
        {
            CurrStadium = new Stadium(2000) ;
     
            fanGenerator = new Thread(FanGenerator);
            GenerateFunSleep = 500;
            random = new Random();        

            _sectorPlases = new Dictionary<int, ushort>();
            for (int i = 0; i < CurrStadium.CountSectors; i++)
            {
                _sectorPlases.Add(i, 0);
            }
            InitializeComponent();
            CreateSectorsStatisticElements();
            ResetData();
        }

        public void CreateSectorsStatisticElements()
        {
            spStadiumStatistic.Children.Clear();
            for (int i = 0; i < CurrStadium.CountSectors; i++)
            {
                DockPanel dockPanel = new DockPanel();
                TextBlock tbStatisticName = new TextBlock();
                tbStatisticName.Text = $"Sector {i} busy places: ";
                DockPanel.SetDock(tbStatisticName, Dock.Left);
                dockPanel.Children.Add(tbStatisticName);
                TextBlock tbStatisticValue = new TextBlock();
                DockPanel.SetDock(tbStatisticValue, Dock.Right);
                Binding myBinding = new Binding();
                myBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1);
                myBinding.Path = new PropertyPath($"CurrStadium.SectorsPlaces[{i}].FansInSector.Count");
                myBinding.Mode = BindingMode.OneWay;
                myBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                tbStatisticValue.SetBinding(TextBlock.TextProperty, myBinding);
                dockPanel.Children.Add(tbStatisticValue);
                spStadiumStatistic.Children.Add(dockPanel);

                dockPanel = new DockPanel();
                tbStatisticName = new TextBlock();
                tbStatisticName.Text = $"Sector {i} Free places: ";
                DockPanel.SetDock(tbStatisticName, Dock.Left);
                dockPanel.Children.Add(tbStatisticName);
                tbStatisticValue = new TextBlock();
                DockPanel.SetDock(tbStatisticValue, Dock.Right);
                 myBinding = new Binding();
                myBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1);
                myBinding.Path = new PropertyPath($"CurrStadium.SectorsPlaces[{i}].FreePlaces");
                myBinding.Mode = BindingMode.OneWay;
                myBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                tbStatisticValue.SetBinding(TextBlock.TextProperty, myBinding);
                dockPanel.Children.Add(tbStatisticValue);
                               
                spStadiumStatistic.Children.Add(dockPanel);
            }            
        }

        public void ResetData()
        {
            _sectorPlases = new Dictionary<int, ushort>();
            for (int i = 0; i < CurrStadium.CountSectors; i++)
            {
                _sectorPlases.Add(i, 0);
            }
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {          
            if (!fanGenerator.IsAlive)
            {
                ResetData();
                fanGenerator = new Thread(FanGenerator);
                fanGenerator.Start();
            CurrStadium.StartServicingVisitors();
            }
        }

        public static Random random;

        public ushort GetFreeSector(ushort countSectors, ushort countPlases)
        {
            List<int> sectors = new List<int>();
            for (int i = 0; i < countSectors; i++)
            {
                sectors.Add(i);
            }
            while (true)
            {
                if (sectors.Count == 0)
                {
                    throw new Exception("AllTicketSold");
                }
                ushort trySector = (ushort)random.Next(sectors.Count);
                bool NiceSector = false;
                this.Dispatcher.Invoke(() =>
                {
                    if (_sectorPlases[sectors[trySector]] < CurrStadium.SectorsPlaces[sectors[trySector]].CountPlaces)
                    {
                        NiceSector = true;                    
                    }
                    else
                    {
                        sectors.RemoveAt(trySector);
                    }
                });
                if(NiceSector)
                return (ushort)sectors[trySector];
            }
        }

        private Fan GenerateFan(ushort countSectors, ushort countPlases)
        {

            Fan fan = null;
            if (Convert.ToBoolean(random.Next(3)))
            {
                ushort sectorNumber = GetFreeSector(countSectors, countPlases);
                fan = new Fan() { Name = "Fan", HasTicket = true, PlaceNumber = _sectorPlases[sectorNumber]++, SectorNumber = sectorNumber };
            }
            else
            {
                fan = new Fan() { Name = "FakeFan", HasTicket = false, PlaceNumber = 123, SectorNumber = 123 };
            }

            return fan;
        }
        private void FanGenerator()
        {
            try
            {
                bool isStadiumFull = false;
                while (!isStadiumFull)
                {
                    ushort countPlaces = 0;
                    ushort countSectors = 0;

                    this.Dispatcher.Invoke(() =>
                {
                    countSectors = CurrStadium.CountSectors;
                    countPlaces = (ushort)(CurrStadium.CountPlaces / CurrStadium.CountSectors);
                    CurrStadium.IsStadiumFull();
                    isStadiumFull = CurrStadium.IsStadiumFull();
                });
                    Fan fan = GenerateFan(countSectors, countPlaces);

                    int generateFunSleep=0;
                    this.Dispatcher.Invoke(() =>
                    {
                        CurrStadium.EntranceStadiumQueue.Add(fan);
                        generateFunSleep = GenerateFunSleep;
                    });
                    Thread.Sleep(generateFunSleep);
                }
            }
            catch (ThreadAbortException ex)
            {
                MessageBox.Show(ex.Message + " FanGenerator.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fanGenerator.Abort();
                CurrStadium.ResetData();              
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner=this;
            settingsWindow.Show();
        }

        private void myUpDownControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            CurrStadium.CountPlacesChanged();
        }
    }
}
