using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Exam_stadium_threads.StadiumRoot.StadiumClasses
{
    public class Sector : DependencyObject
    {
        public int Id { get; set; }

        public static readonly DependencyProperty CountPlacesProperty;
        public ushort CountPlaces
        {
            get { return (ushort)GetValue(CountPlacesProperty); }
            set
            {
                SetValue(CountPlacesProperty, value);
                UbdateFreePlaces();
            }
        }

        public static readonly DependencyProperty BusyPlacesProperty;


        public static readonly DependencyProperty FreePlacesProperty;
        public ushort FreePlaces
        {
            get { return (ushort)GetValue(FreePlacesProperty); }
            set { SetValue(FreePlacesProperty, value); }
        }
        private ObservableCollection<Fan> _fansInSector;
        public ObservableCollection<Fan> FansInSector
        {
            get
            {
                return _fansInSector;
            }
            set
            {
                _fansInSector = value;
             
            }
        }

        static Sector()
        {
            CountPlacesProperty = DependencyProperty.Register("CountPlaces", typeof(ushort), typeof(Sector));
            BusyPlacesProperty = DependencyProperty.Register("BusyPlaces", typeof(ushort), typeof(Sector));
            FreePlacesProperty = DependencyProperty.Register("FreePlaces", typeof(ushort), typeof(Sector));
        }

        public Sector()
        {
            FansInSector = new ObservableCollection<Fan>();
            FansInSector.CollectionChanged += UbdateFreePlaces;
        }

        private void UbdateFreePlaces(object sender, NotifyCollectionChangedEventArgs e)
        {

            UbdateFreePlaces();
        }
        private void UbdateFreePlaces()
        {
            FreePlaces = (ushort)(CountPlaces - FansInSector.Count);
        }
    }
}
