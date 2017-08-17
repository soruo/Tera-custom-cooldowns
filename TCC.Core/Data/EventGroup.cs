﻿using System.Windows.Threading;

namespace TCC.ViewModels
{
    public class EventGroup : TSPropertyChanged
    {
        public SynchronizedObservableCollection<DailyEvent> Events { get; }
        public string Name { get; }
        public bool RemoteCheck { get; }
        public EventGroup(string name)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Events = new SynchronizedObservableCollection<DailyEvent>(_dispatcher);
            Name = name;
            RemoteCheck = Name == "gbam";
        }
        public void AddEvent(DailyEvent ev)
        {
            Events.Insert(0, ev);
        }
    }
}