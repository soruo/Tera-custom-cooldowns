﻿using FoglioUtils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using TCC.Controls;
using TCC.Data;
using TCC.Data.Pc;
using TCC.Interop.Proxy;
using TCC.Parsing;
using TCC.Settings;
using TCC.Utils;
using TCC.ViewModels.Widgets;
using TeraDataLite;
using TeraPacketParser.Messages;

namespace TCC.ViewModels
{

    [TccModule]
    public class LfgListViewModel : TccWindowViewModel
    {
        public event Action<int> Publicized;
        public static DispatcherTimer RequestTimer;
        private DispatcherTimer PublicizeTimer;
        private DispatcherTimer AutoPublicizeTimer;
        public static Queue<uint> RequestQueue = new Queue<uint>();
        private bool _creating;
        private int _lastGroupSize = 0;
        public Listing LastClicked;
        private string _newMessage;
        public string LastSortDescr { get; set; } = "Message";
        public int PublicizeCooldown => 5;
        public int AutoPublicizeCooldown => 30;
        public void RefreshSorting()
        {
            SortCommand.Refresh(LastSortDescr);
        }
        public bool IsPublicizeEnabled => !PublicizeTimer.IsEnabled;
        public bool IsAutoPublicizeOn => AutoPublicizeTimer.IsEnabled;
        private bool _stopAuto;
        public SynchronizedObservableCollection<Listing> Listings { get; }
        public SortCommand SortCommand { get; }
        public RelayCommand PublicizeCommand { get; }
        public RelayCommand ToggleAutoPublicizeCommand { get; }
        public ICollectionViewLiveShaping ListingsView { get; }
        public bool Creating
        {
            get => _creating;
            set
            {
                if (_creating == value) return;
                _creating = value;
                N();
            }
        }
        public string NewMessage
        {
            get => _newMessage;
            set
            {
                if (_newMessage == value) return;
                _newMessage = value;
                N();
            }
        }
        public bool AmIinLfg => Dispatcher.Invoke(() => Listings.ToSyncList().Any(listing => listing.LeaderId == Game.Me.PlayerId
                                                                                              || listing.LeaderName == Game.Me.Name
                                                                                              || listing.Players.ToSyncList().Any(player => player.PlayerId == Game.Me.PlayerId)
                                                                                              || WindowManager.ViewModels.GroupVM.Members.ToSyncList().Any(member => member.PlayerId == listing.LeaderId)));
        public void NotifyMyLfg()
        {
            N(nameof(AmIinLfg));
            N(nameof(MyLfg));
            foreach (var listing in Listings.ToSyncList())
            {
                listing?.UpdateIsMyLfg();
            }
            MyLfg?.UpdateIsMyLfg();
        }
        public bool AmILeader => WindowManager.ViewModels.GroupVM.AmILeader;
        public Listing MyLfg => Dispatcher.Invoke(() => Listings.FirstOrDefault(listing => listing.Players.Any(p => p.PlayerId == Game.Me.PlayerId)
                                                                   || listing.LeaderId == Game.Me.PlayerId
                                                                   || WindowManager.ViewModels.GroupVM.Members.ToSyncList().Any(member => member.PlayerId == listing.LeaderId)
                                                             ));

        public bool StayClosed { get; set; }

        public LfgListViewModel(LfgWindowSettings settings) : base(settings)
        {
            KeyboardHook.Instance.RegisterCallback(App.Settings.LfgHotkey, OnShowLfgHotkeyPressed);
            Listings = new SynchronizedObservableCollection<Listing>(Dispatcher);
            ListingsView = CollectionViewUtils.InitLiveView(Listings, null, new string[] { }, new SortDescription[] { });
            SortCommand = new SortCommand(ListingsView);
            Listings.CollectionChanged += ListingsOnCollectionChanged;
            settings.HideTradeListingsChangedEvent += OnHideTradeChanged;
            if(WindowManager.ViewModels.GroupVM != null) WindowManager.ViewModels.GroupVM.PropertyChanged += OnGroupWindowVmPropertyChanged;
            RequestTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, RequestNextLfg, Dispatcher);
            PublicizeTimer = new DispatcherTimer(TimeSpan.FromSeconds(PublicizeCooldown), DispatcherPriority.Background, OnPublicizeTimerTick, Dispatcher) { IsEnabled = false };
            AutoPublicizeTimer = new DispatcherTimer(TimeSpan.FromSeconds(AutoPublicizeCooldown), DispatcherPriority.Background, OnAutoPublicizeTimerTick, Dispatcher) { IsEnabled = false };
            PublicizeCommand = new RelayCommand(Publicize, CanPublicize);
            ToggleAutoPublicizeCommand = new RelayCommand(ToggleAutoPublicize, CanToggleAutoPublicize);
        }

        private void OnShowLfgHotkeyPressed()
        {
            if (!Game.Logged) return;
            if (!ProxyInterface.Instance.IsStubAvailable) return;
            if (!WindowManager.LfgListWindow.IsVisible)
            {
                StayClosed = false;
                ProxyInterface.Instance.Stub.RequestListings();
            }
            else WindowManager.LfgListWindow.HideWindow();
        }

        private void OnAutoPublicizeTimerTick(object sender, EventArgs e)
        {
            if (Game.IsInDungeon || !AmIinLfg) _stopAuto = true;

            if (_stopAuto)
            {
                AutoPublicizeTimer.Stop();
                N(nameof(IsAutoPublicizeOn)); //notify UI that CanPublicize changed
                N(nameof(IsPublicizeEnabled)); //notify UI that CanPublicize changed
                _stopAuto = false;
            }
            else
            {
                if (!/*ProxyOld.IsConnected */ ProxyInterface.Instance.IsStubAvailable) return;
                ProxyInterface.Instance.Stub.PublicizeListing(); //ProxyOld.PublicizeLfg();
                Publicized?.Invoke(AutoPublicizeCooldown);
            }
        }

        private void OnPublicizeTimerTick(object sender, EventArgs e)
        {
            PublicizeTimer.Stop();
            N(nameof(IsPublicizeEnabled)); //notify UI that CanPublicize changed
        }

        private void Publicize(object obj)
        {
            if (Game.IsInDungeon) return;
            PublicizeTimer.Start();
            N(nameof(IsPublicizeEnabled)); //notify UI that CanPublicize changed
            if (!/*ProxyOld.IsConnected */ ProxyInterface.Instance.IsStubAvailable) return;
            ProxyInterface.Instance.Stub.PublicizeListing(); //ProxyOld.PublicizeLfg();
            Publicized?.Invoke(PublicizeCooldown);

        }
        private bool CanPublicize(object arg)
        {
            return IsPublicizeEnabled && !IsAutoPublicizeOn;
        }
        private void ToggleAutoPublicize(object obj)
        {
            if (IsAutoPublicizeOn)
            {
                _stopAuto = true;
            }
            else
            {
                if (!AmIinLfg)
                {
                    _stopAuto = true;
                    return;
                }
                AutoPublicizeTimer.Start();
                N(nameof(IsAutoPublicizeOn)); //notify UI that CanPublicize changed
                N(nameof(IsPublicizeEnabled)); //notify UI that CanPublicize changed
                if (!/*ProxyOld.IsConnected */ ProxyInterface.Instance.IsStubAvailable) return;
                ProxyInterface.Instance.Stub.PublicizeListing(); //ProxyOld.PublicizeLfg();
                Publicized?.Invoke(AutoPublicizeCooldown);
            }
        }
        private bool CanToggleAutoPublicize(object arg)
        {
            return /*ProxyOld.IsConnected */ ProxyInterface.Instance.IsStubAvailable &&
                   !Game.LoadingScreen &&
                   Game.Logged &&
                   !Game.IsInDungeon;
        }


        private void RequestNextLfg(object sender, EventArgs e)
        {
            if (!App.Settings.LfgWindowSettings.Enabled) return;
            if (RequestQueue.Count == 0) return;

            var req = RequestQueue.Dequeue();
            if (req == 0)
            {
                StayClosed = true;
                ProxyInterface.Instance.Stub.RequestListings(); //ProxyOld.RequestLfgList();
            }
            else
            {
                ProxyInterface.Instance.Stub.RequestPartyInfo(req); //ProxyOld.RequestPartyInfo(req);
            }
        }

        public void EnqueueRequest(uint id)
        {
            if ((Game.IsInDungeon || Game.CivilUnrestZone) && Game.Combat) return;
            Dispatcher.Invoke(() =>
            {
                if (RequestQueue.Count > 0 && RequestQueue.Last() == id) return;
                RequestQueue.Enqueue(id);
            });
        }

        private void OnGroupWindowVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GroupWindowViewModel.AmILeader)) N(nameof(AmILeader));
        }

        private void ListingsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //NotifyMyLfg();
            Task.Delay(500).ContinueWith(t => { Dispatcher.Invoke(NotifyMyLfg); });

        }

        internal void RemoveDeadLfg()
        {
            Listings.Remove(LastClicked);
        }

        public void EnqueueListRequest()
        {
            Dispatcher.Invoke(() =>
            {
                if (RequestQueue.Count > 0 && RequestQueue.Last() == 0) return;
                RequestQueue.Enqueue(0);
            });

        }

        public void ForceStopPublicize()
        {
            PublicizeTimer.Stop();
            AutoPublicizeTimer.Stop();
            N(nameof(IsPublicizeEnabled)); //notify UI that CanPublicize changed
            N(nameof(IsAutoPublicizeOn)); //notify UI that CanPublicize changed
        }

        private void SyncListings(List<ListingData> listings)
        {
            Task.Factory.StartNew(() =>
            {
                listings.ForEach(l => Dispatcher.InvokeAsync(() => { AddOrRefreshListing(l);}));
                RemoveMissingListings();
            });



            void AddOrRefreshListing(ListingData l)
            {
                if (Listings.Any(toFind => toFind.LeaderId == l.LeaderId))
                {
                    var target = Listings.FirstOrDefault(t => t.LeaderId == l.LeaderId);
                    if (target == null) return;
                    target.LeaderId = l.LeaderId;
                    target.Message = l.Message;
                    target.IsRaid = l.IsRaid;
                    target.LeaderName = l.LeaderName;
                    if (target.PlayerCount != l.PlayerCount)
                    {
                        EnqueueRequest(l.LeaderId);
                    }
                }
                else
                {
                    if (l.IsTrade && ((LfgWindowSettings)Settings).HideTradeListings) return;
                    Listings.Add(new Listing(l));
                    EnqueueRequest(l.LeaderId);
                }
            }
            void RemoveMissingListings()
            {
                var toRemove = new List<uint>();

                Listings.ToSyncList().ForEach(l =>
                {
                    if (listings.All(f => f.LeaderId != l.LeaderId)) toRemove.Add(l.LeaderId);
                });
                toRemove.ForEach(r =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        var target = Listings.FirstOrDefault(rm => rm.LeaderId == r);
                        if (target != null) Listings.Remove(target);
                    });
                });
            }
        }

        private void OnHideTradeChanged()
        {
            if (!((LfgWindowSettings)Settings).HideTradeListings) return;
            var toRemove = Listings.ToSyncList().Where(l => l.IsTrade).Select(s => s.LeaderId).ToList();
            toRemove.ForEach(r =>
            {
                var target = Listings.FirstOrDefault(rm => rm.LeaderId == r);
                if (target != null) Listings.Remove(target);
            });
        }
        protected override void InstallHooks()
        {
            PacketAnalyzer.Processor.Hook<S_LOGIN>(OnLogin);
            PacketAnalyzer.Processor.Hook<S_RETURN_TO_LOBBY>(OnReturnToLobby);
            PacketAnalyzer.Processor.Hook<S_SHOW_PARTY_MATCH_INFO>(OnShowPartyMatchInfo);
            PacketAnalyzer.Processor.Hook<S_OTHER_USER_APPLY_PARTY>(OnOtherUserApplyParty);
            PacketAnalyzer.Processor.Hook<S_PARTY_MEMBER_LIST>(OnPartyMemberList);
            PacketAnalyzer.Processor.Hook<S_LEAVE_PARTY>(OnLeaveParty);
            PacketAnalyzer.Processor.Hook<S_BAN_PARTY>(OnBanParty);
            PacketAnalyzer.Processor.Hook<S_PARTY_MEMBER_INFO>(OnPartyMemberInfo);
            PacketAnalyzer.Processor.Hook<S_SHOW_CANDIDATE_LIST>(OnShowCandidateList);
        }

        protected override void RemoveHooks()
        {
            PacketAnalyzer.Processor.Unhook<S_LOGIN>(OnLogin);
            PacketAnalyzer.Processor.Unhook<S_RETURN_TO_LOBBY>(OnReturnToLobby);
            PacketAnalyzer.Processor.Unhook<S_SHOW_PARTY_MATCH_INFO>(OnShowPartyMatchInfo);
            PacketAnalyzer.Processor.Unhook<S_OTHER_USER_APPLY_PARTY>(OnOtherUserApplyParty);
            PacketAnalyzer.Processor.Unhook<S_PARTY_MEMBER_LIST>(OnPartyMemberList);
            PacketAnalyzer.Processor.Unhook<S_LEAVE_PARTY>(OnLeaveParty);
            PacketAnalyzer.Processor.Unhook<S_BAN_PARTY>(OnBanParty);
            PacketAnalyzer.Processor.Unhook<S_PARTY_MEMBER_INFO>(OnPartyMemberInfo);
            PacketAnalyzer.Processor.Unhook<S_SHOW_CANDIDATE_LIST>(OnShowCandidateList);
        }

        private void OnLogin(S_LOGIN m)
        {
            Listings.Clear();
            EnqueueListRequest(); // need invoke?
        }
        private void OnShowCandidateList(S_SHOW_CANDIDATE_LIST p)
        {
            if (MyLfg == null) return;

            var dest = MyLfg.Applicants;
            foreach (var applicant in p.Candidates)
            {
                if (dest.All(x => x.PlayerId != applicant.PlayerId)) dest.Add(new User(applicant));
            }

            var toRemove = new List<User>();
            foreach (var user in dest)
            {
                if (p.Candidates.All(x => x.PlayerId != user.PlayerId)) toRemove.Add(user);
            }

            toRemove.ForEach(r => dest.Remove(r));
        }
        private void OnPartyMemberInfo(S_PARTY_MEMBER_INFO m)
        {
            //if (!App.Settings.LfgWindowSettings.Enabled) return;
            var lfg = Listings.FirstOrDefault(listing => listing.LeaderId == m.Id || m.Members.Any(member => member.PlayerId == listing.LeaderId));
            if (lfg == null) return;
            Task.Factory.StartNew(() =>
            {

            m.Members.ForEach(member =>
            {
                if (lfg.Players.Any(toFind => toFind.PlayerId == member.PlayerId))
                {
                    var target = lfg.Players.FirstOrDefault(player => player.PlayerId == member.PlayerId);
                    if (target == null) return;
                    target.IsLeader = member.IsLeader;
                    target.Online = member.Online;
                    target.Location = Game.DB.GetSectionName(member.GuardId, member.SectionId);
                }
                else
                    lfg.Players.Add(new User(member));
            });

            var toDelete = new List<uint>();
            lfg.Players.ToList()
                .ForEach(player =>
                {
                    if (m.Members.All(newMember => newMember.PlayerId != player.PlayerId)) toDelete.Add(player.PlayerId);
                    toDelete.ForEach(targetId => lfg.Players.Remove(lfg.Players.FirstOrDefault(playerToRemove => playerToRemove.PlayerId == targetId)));
                });

            lfg.LeaderId = m.Id;
            var leader = lfg.Players.FirstOrDefault(u => u.IsLeader);
            if (leader != null) lfg.LeaderName = leader.Name;
            if (LastClicked != null && LastClicked.LeaderId == lfg.LeaderId) lfg.IsExpanded = true;
            lfg.PlayerCount = m.Members.Count;
            NotifyMyLfg();
            });
        }
        private void OnLeaveParty(S_LEAVE_PARTY m)
        {
            NotifyMyLfg();
        }
        private void OnBanParty(S_BAN_PARTY m)
        {
            NotifyMyLfg();
        }
        private void OnPartyMemberList(S_PARTY_MEMBER_LIST m)
        {
            if (_lastGroupSize == 0) NotifyMyLfg();
            _lastGroupSize = m.Members.Count;
            if (!ProxyInterface.Instance.IsStubAvailable || !App.Settings.LfgWindowSettings.Enabled || !Game.InGameUiOn) return;
            ProxyInterface.Instance.Stub.RequestListingCandidates();
            if (WindowManager.LfgListWindow == null || !WindowManager.LfgListWindow.IsVisible) return;
            ProxyInterface.Instance.Stub.RequestListings();
        }
        private void OnOtherUserApplyParty(S_OTHER_USER_APPLY_PARTY m)
        {
            //if (!App.Settings.LfgWindowSettings.Enabled) return;
            if (MyLfg == null) return;
            var dest = MyLfg.Applicants;
            if (dest.Any(u => u.PlayerId == m.PlayerId)) return;
            dest.Add(new User(Dispatcher)
            {
                PlayerId = m.PlayerId,
                UserClass = m.Class,
                Level = Convert.ToUInt32(m.Level),
                Name = m.Name,
                Online = true
            });
        }
        private void OnShowPartyMatchInfo(S_SHOW_PARTY_MATCH_INFO m)
        {
            //if (!App.Settings.LfgWindowSettings.Enabled) return; //
            if (!m.IsLast && ProxyInterface.Instance.IsStubAvailable) ProxyInterface.Instance.Stub.RequestListingsPage(m.Page + 1);

            if (!m.IsLast) return;

            if (S_SHOW_PARTY_MATCH_INFO.Listings.Count != 0) SyncListings(S_SHOW_PARTY_MATCH_INFO.Listings);

            NotifyMyLfg();
            WindowManager.LfgListWindow.ShowWindow();
        }
        private void OnReturnToLobby(S_RETURN_TO_LOBBY m)
        {
            ForceStopPublicize();
        }
    }

    public class SortCommand : ICommand
    {
        private readonly ICollectionViewLiveShaping _view;
        private bool _refreshing;
        private ListSortDirection _direction = ListSortDirection.Ascending;
#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var f = (string)parameter;
            if (!_refreshing) _direction = _direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            ((CollectionView)_view).SortDescriptions.Clear();
            ((CollectionView)_view).SortDescriptions.Add(new SortDescription(f, _direction));
            WindowManager.ViewModels.LfgVM.LastSortDescr = parameter.ToString();
        }
        public SortCommand(ICollectionViewLiveShaping view)
        {
            _view = view;
        }

        public void Refresh(string lastSortDescr)
        {
            _refreshing = true;
            Execute(lastSortDescr);
            _refreshing = false;
        }
    }

}
