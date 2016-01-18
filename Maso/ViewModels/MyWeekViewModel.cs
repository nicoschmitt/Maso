using Caliburn.Micro;
using Maso.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Maso.ViewModels
{
    public class MyWeekViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;

        private int number;
        private bool allDone, noCoach;

        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                this.NotifyOfPropertyChange(() => Number);
                this.NotifyOfPropertyChange(() => Title);
            }
        }

        public string Title
        {
            get
            {
                return "Week " + Number;
            }
        }

        public bool AllDone
        {
            get { return allDone; }
            set
            {
                allDone = value;
                this.NotifyOfPropertyChange(() => AllDone);
            }
        }

        public bool NoCoach
        {
            get { return noCoach; }
            set
            {
                noCoach = value;
                this.NotifyOfPropertyChange(() => NoCoach);
            }
        }

        public BindableCollection<TrainingViewModel> Trainings
        {
            get;
            private set;
        }
        
        public MyWeekViewModel() : this(null, null) { }

        public MyWeekViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;

            AllDone = NoCoach = false;
            Trainings = new BindableCollection<TrainingViewModel>();
#if DEBUG
            if (Execute.InDesignMode)
            {
                Number = 4;

                var w = new BindableCollection<WorkoutViewModel>();
                w.Add(new WorkoutViewModel() { IdxWeek = 1, Name = "apollon", Title = "APOLLON", WorkoutType = "Standard", Time = "3:04" });
                w.Add(new WorkoutViewModel() { IdxWeek = 2, Name = "gaia", Title = "6/10 GAIA", WorkoutType = "Standard", Active = true });
                w.Add(new WorkoutViewModel() { IdxWeek = 3, Name = "dione", Title = "DIONE", WorkoutType = "Standard" });

                Trainings.Add(new TrainingViewModel() { Workouts = w });
            }
#endif
            
        }

        protected async override void OnActivate()
        {
            base.OnActivate();

            if (dataservice != null)
            {
                await RefreshData(true);

                await SendCachedCompletion();
            }
        }

        protected async Task SendCachedCompletion()
        {
            try
            {
                var storage = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                if (storage.ContainsKey("sendcache"))
                {
                    var sendcache = JsonConvert.DeserializeObject<List<Completion>>((string)storage["sendcache"]);
                    storage["sendcache"] = null;
                    foreach (var session in sendcache)
                    {
                        try
                        {
                            await dataservice.SendCompletion(session);
                        }
                        catch (SendLaterException) { }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        protected async Task RefreshData(bool fromcache = false)
        {
            MyWeekViewModel data = null;
            if (fromcache)
            {
                data = await dataservice.GetMyWeekFromCache();
            }
            else
            {
                data = await dataservice.GetMyWeek();
            }

            if (data != null)
            {
                this.Number = data.Number;
                this.Trainings.Clear();
                this.Trainings.AddRange(data.Trainings);

                AllDone = true;
                foreach (var t in this.Trainings)
                {
                    foreach (var w in t.Workouts)
                    {
                        if (string.IsNullOrEmpty(w.Time)) AllDone = false;
                    }
                }
            }
            else
            {
                NoCoach = true;
            }
        }

        protected void GotoNews()
        {
            navigationService.NavigateToViewModel<PeopleViewModel>();
        }

        protected void GotoFreeTraining()
        {
            navigationService.NavigateToViewModel<FreeTrainingViewModel>();
        }

        protected async void FinishWeek()
        {
            try
            {
                await dataservice.SendFinishWeek(number);
                await RefreshData(false);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        protected void Disconnect()
        {
            dataservice.Disconnect();
            navigationService.NavigateToViewModel<LoadingViewModel>();
        }

        protected void About()
        {
            navigationService.NavigateToViewModel<AboutViewModel>();
        }
    }
}
