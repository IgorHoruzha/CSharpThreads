using Exam_stadium_threads.StadiumRoot.StadiumClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Exam_stadium_threads.StadiumRoot
{
    public class Stadium : DependencyObject
    {
        public string StadiumName { get; set; }

        public static readonly DependencyProperty CountPlacesProperty;
        public ushort CountPlaces
        {
            get { return (ushort)GetValue(CountPlacesProperty); }
            set
            {
                CountPlacesChanged();
                SetValue(CountPlacesProperty, value);
            }
        }

        public static readonly DependencyProperty FreePlacesProperty;
        public ushort FreePlaces
        {
            get { return (ushort)GetValue(FreePlacesProperty); }
            set { SetValue(FreePlacesProperty, value); }
        }

        public short CountSecurity { get; set; } = 4;
        public int SecuritySleep { get; set; } = 4000;

        private List<Thread> _securityThreads;

        static ManualResetEvent _nextStewardEvent = new ManualResetEvent(true);

        private static object _stewardLocker = new object();
        public short CountSteward { get; set; } = 6;
        public int StewardSleep { get; set; } = 8000;

        private List<Thread> _stewardThreads;

        public static readonly DependencyProperty BusyPlacesProperty;

        private static object _busyPlasesLocker = new object();

        public ushort BusyPlaces
        {
            get
            {
                lock (_busyPlasesLocker)
                {
                    return (ushort)GetValue(BusyPlacesProperty);
                }
            }
            set
            {
                lock (_busyPlasesLocker)
                {
                    SetValue(BusyPlacesProperty, value);
                    RefreshFreePlaces();
                }
            }
        }

        public void RefreshFreePlaces()
        {
            FreePlaces = (ushort)(CountPlaces - BusyPlaces);
        }

        public ushort CountSectors { get; set; } = 6;

        public ushort StadiumEntrance { get; set; } = 4;

        private static object _entranceStadiumQueueLocker = new object();

        private ObservableCollection<Fan> _entranceStadiumQueue;
        public ObservableCollection<Fan> EntranceStadiumQueue
        {
            get
            {
                return _entranceStadiumQueue;
            }
            set
            {
                _entranceStadiumQueue = value;
            }
        }

        static object _entranceSectorsLocker = new object();

        public Dictionary<int, ObservableCollection<Fan>> EntranceSectorsQueue { get; set; }

        private object _sectorsPlacesLocker = new object();

        public Dictionary<int, Sector> SectorsPlaces { get; set; }


        static Stadium()
        {
            CountPlacesProperty = DependencyProperty.Register("CountPlaces", typeof(ushort), typeof(Stadium));
            BusyPlacesProperty = DependencyProperty.Register("BusyPlaces", typeof(ushort), typeof(Stadium));
            FreePlacesProperty = DependencyProperty.Register("FreePlaces", typeof(ushort), typeof(Stadium));
        }


        public Stadium(ushort countPlaces)
        {
            SectorsPlaces = new Dictionary<int, Sector>();
            for (ushort i = 0; i < CountSectors; i++)
            {
                Sector sector = new Sector()
                {
                    Id = i,
                    CountPlaces = (ushort)(_actualSectorPlaces / CountSectors + BonusSectorPlaces)
                };
                SectorsPlaces.Add(sector.Id, sector);
            }
            CountPlaces = countPlaces;
            GeneratePlaces();
            EntranceStadiumQueue = new ObservableCollection<Fan>();

            _securityThreads = new List<Thread>();

            for (int i = 0; i < CountSecurity; i++)
            {
                _securityThreads.Add(new Thread(CheckTicket) { Name = $"Security {i}." });
            }

            _stewardThreads = new List<Thread>();

            for (int i = 0; i < CountSteward; i++)
            {
                _stewardThreads.Add(new Thread(FanAccommodationBySector) { Name = $"Steward {i}." });
            }

            EntranceSectorsQueue = new Dictionary<int, ObservableCollection<Fan>>();
            for (ushort i = 0; i < StadiumEntrance; i++)
            {
                EntranceSectorsQueue[i] = new ObservableCollection<Fan>();
            }
        }

        public void CountPlacesChanged()
        {
            GeneratePlaces();
            foreach (var SectorPlace in SectorsPlaces)
            {
                SectorPlace.Value.CountPlaces = (ushort)(_actualSectorPlaces / CountSectors + BonusSectorPlaces);
            }
        }
        public void AbortThreads()
        {
            foreach (var item in _securityThreads)
            {
                item.Abort();
            }
            foreach (var item in _stewardThreads)
            {
                item.Abort();
            }
        }
        public void ResetData()
        {
            AbortThreads();
            BusyPlaces = 0;
            EntranceStadiumQueue.Clear();
            _securityThreads = new List<Thread>();
            for (int i = 0; i < CountSecurity; i++)
            {
                _securityThreads.Add(new Thread(CheckTicket) { Name = $"Security {i}." });
            }

            _stewardThreads = new List<Thread>();
            for (int i = 0; i < CountSteward; i++)
            {
                _stewardThreads.Add(new Thread(FanAccommodationBySector) { Name = $"Steward {i}." });
            }

            for (ushort i = 0; i < StadiumEntrance; i++)
            {
                EntranceSectorsQueue[i].Clear();
            }

            for (ushort i = 0; i < CountSectors; i++)
            {
                SectorsPlaces[i].FansInSector.Clear();
            }
        }
        private ushort _bonusSectorPlaces;

        public ushort BonusSectorPlaces
        {
            get
            {
                if (_bonusSectorPlaces > 0)
                {
                    _bonusSectorPlaces--;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set { _bonusSectorPlaces = value; }
        }

        private ushort _actualSectorPlaces;
        private void GeneratePlaces()
        {
            _bonusSectorPlaces = (ushort)(CountPlaces % CountSectors);
            _actualSectorPlaces = (ushort)(CountPlaces - _bonusSectorPlaces);
        }

        private void FanAccommodationBySector()
        {

            try
            {
                bool isStadiumFull = false;
                int countQueue = 0;
                this.Dispatcher.Invoke(() =>
                {
                    countQueue = StadiumEntrance;
                });

                while (!isStadiumFull)
                {
                    Fan fan = null;
                    int mostLongQueueIndex = 0;
                    int mostLongQueueLength = int.MinValue;
                    _nextStewardEvent.WaitOne();          
                    for (int i = 0; i < countQueue; i++)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                          
                            if (EntranceSectorsQueue[i].Count > mostLongQueueLength)
                            {
                                mostLongQueueIndex = i;
                                mostLongQueueLength = EntranceSectorsQueue[i].Count;
                                
                            }
                          
                           
                        });
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                      
                        lock (_sectorsPlacesLocker)
                        {
                            if (EntranceSectorsQueue[mostLongQueueIndex].Count > 0)
                            {
                                fan = EntranceSectorsQueue[mostLongQueueIndex].First<Fan>();
                                EntranceSectorsQueue[mostLongQueueIndex].Remove(fan);
                                SectorsPlaces[fan.SectorNumber].FansInSector.Add(fan);

                                //TODO:  Danger LOAD LOAD FOR THE PROGRAM. 
                                //  Perhaps it would be better to do the sorting at the end of filling the stadium.
                                var sortableList = new List<Fan>(SectorsPlaces[fan.SectorNumber].FansInSector);
                                sortableList.Sort(comparison);
                                for (int i = 0; i < sortableList.Count; i++)
                                {
                                    SectorsPlaces[fan.SectorNumber].FansInSector.Move(SectorsPlaces[fan.SectorNumber].FansInSector.IndexOf(sortableList[i]), i);
                                }
                                //===============================================
                                BusyPlaces++;
                            }
                            else
                            {
                                Debug.WriteLine(mostLongQueueIndex);
                                Debug.WriteLine("Error");
                            }
                            isStadiumFull = IsStadiumFull();
                            _nextStewardEvent.Set();
                        }
                       
                    });
                    Thread.Sleep(StewardSleep);
                }
            }
            catch (ThreadAbortException)
            {
               
            }
            finally
            {
                MessageBox.Show(Thread.CurrentThread.Name + "  EndWork");
            }

        }

        private int comparison(Fan x, Fan y)
        {
            return x.PlaceNumber - y.PlaceNumber;
        }

        public void StartServicingVisitors()
        {
            ResetData();
            for (int i = 0; i < CountSecurity; i++)
            {
                if (!_securityThreads[i].IsAlive)
                {
                    _securityThreads[i].Start();
                }
            }

            for (int i = 0; i < CountSteward; i++)
            {
                if (!_stewardThreads[i].IsAlive)
                {
                    _stewardThreads[i].Start();
                }
            }
        }

        private static object _ticketValidationEntranceLocker = new object();

        private static object _ticketValidationSectorLocker = new object();
        public bool IsTicketValid(Fan fan)
        {

            if (fan.HasTicket && !IsStadiumFull())
            {
                foreach (var entranceSectorsQueue in EntranceSectorsQueue)
                {
                    lock (_ticketValidationEntranceLocker)
                    {
                        if (entranceSectorsQueue.Value.Any((Fan f1) => { return f1.PlaceNumber == fan.PlaceNumber && f1.SectorNumber == fan.SectorNumber; }))
                        {
                            return false;
                        }
                    }
                }

                lock (_ticketValidationSectorLocker)
                {
                    if (SectorsPlaces[fan.SectorNumber].FansInSector.Any((Fan f1) => { return f1.PlaceNumber == fan.PlaceNumber && f1.SectorNumber == fan.SectorNumber; }))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }


        public void CheckTicket()
        {
            try
            {

                bool IsStadiumFull = false;
                while (!IsStadiumFull)
                {
                    try
                    {
                        Fan fan = null;
                        bool isTicketValid = false;
                        int EntranceSectorsQueueCount = 0;
                        bool hasFanInFifstQueue = false;
                        this.Dispatcher.Invoke(() =>
                        {
                            hasFanInFifstQueue = EntranceSectorsQueue.Count > 0 ? true : false;
                        });

                        this.Dispatcher.Invoke(() =>
                        {
                            lock (_entranceStadiumQueueLocker)
                            {
                                if (EntranceStadiumQueue.Count > 0)
                                {
                                    fan = EntranceStadiumQueue.Take(1).First<Fan>();
                                    EntranceStadiumQueue.Remove(fan);
                                }
                            }
                            if (fan != null)
                            {
                                isTicketValid = IsTicketValid(fan);//Todo: maybe change later to have better multythread.
                                EntranceSectorsQueueCount = EntranceSectorsQueue.Count;
                            }
                        });

                        if (fan != null && isTicketValid)
                        {
                            int mostShortQueueIndex = 0;
                            int mostShortQueueLength = int.MaxValue;

                            for (int i = 0; i < EntranceSectorsQueueCount; i++)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (EntranceSectorsQueue[i].Count < mostShortQueueLength)
                                    {
                                        mostShortQueueLength = EntranceSectorsQueue[i].Count;
                                        mostShortQueueIndex = i;
                                    }
                                });
                            }
                            this.Dispatcher.Invoke(() =>
                            {
                                if (!this.IsStadiumFull())
                                {
                                    EntranceSectorsQueue[mostShortQueueIndex].Add(fan);
                                }
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                    Thread.Sleep(SecuritySleep);
                    this.Dispatcher.Invoke(() =>
                    {
                        IsStadiumFull = this.IsStadiumFull();
                    });
                }
            }
            catch (ThreadAbortException ex)
            { 
            }
            finally
            {
                MessageBox.Show(Thread.CurrentThread.Name + "  EndWork");
            }
        }

        public bool IsStadiumFull()
        {
            return BusyPlaces >= CountPlaces;
        }
    }
}
