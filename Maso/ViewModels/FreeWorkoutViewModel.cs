using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Maso.ViewModels
{
    public class FreeWorkoutViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;
        private bool loading, display, setup;
        private bool loadAlternative, chooseAlternative;
        private int repetition;
        private WorkoutDetailViewModel selectedWorkout;
        private List<WorkoutDetailViewModel> workoutVariants;

        public BindableCollection<WorkoutDetailViewModel> Workouts
        {
            get;
            private set;
        }

        public string TrainingType { get; set; }

        public bool Loading
        {
            get { return loading; }
            set
            {
                loading = value;
                this.NotifyOfPropertyChange(() => Loading);
            }
        }

        public bool Display
        {
            get { return display; }
            set
            {
                display = value;
                this.NotifyOfPropertyChange(() => Display);
            }
        }
        
        public bool LoadAlternative
        {
            get { return loadAlternative; }
            set
            {
                loadAlternative = value;
                this.NotifyOfPropertyChange(() => LoadAlternative);
            }
        }

        public bool ChooseAlternative
        {
            get { return chooseAlternative; }
            set
            {
                chooseAlternative = value;
                this.NotifyOfPropertyChange(() => ChooseAlternative);
            }
        }

        public bool SetUp
        {
            get { return setup; }
            set
            {
                setup = value;
                this.NotifyOfPropertyChange(() => SetUp);
            }
        }

        public WorkoutDetailViewModel SelectedWorkout
        {
            get { return selectedWorkout; }
            set
            {
                selectedWorkout = value;
                this.NotifyOfPropertyChange(() => SelectedWorkout);
            }
        }

        public IList<WorkoutDetailViewModel> WorkoutVariants
        {
            get { return workoutVariants; }
            set
            {
                workoutVariants = value.ToList();
                this.NotifyOfPropertyChange(() => WorkoutVariants);
            }
        }

        public int Repetition
        {
            get { return repetition; }
            set
            {
                repetition = value;
                this.NotifyOfPropertyChange(() => Repetition);
            }
        }

        public FreeWorkoutViewModel() : this(null, null) { }

        public FreeWorkoutViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;
            Workouts = new BindableCollection<WorkoutDetailViewModel>();

            Loading = true;
            Display = false;
            SetUp = false;
            Repetition = 10;
            LoadAlternative = false;
            ChooseAlternative = true;

            if (Execute.InDesignMode)
            {
                Loading = false;
                Display = true;
                SetUp = true;
                Repetition = 25;
                
                Workouts.Add(new WorkoutDetailViewModel() { Title = "APHRODITE" });
                Workouts.Add(new WorkoutDetailViewModel() { Title = "DIONE" });
                Workouts.Add(new WorkoutDetailViewModel() { Title = "ZEUS" });
            }
        }

        protected async override void OnActivate()
        {
            base.OnActivate();
           
            var workouts = await dataservice.GetAvailableWorkouts(type: TrainingType);
            Workouts.AddRange(workouts);

            Loading = false;
            Display = true;
        }
        
        protected void GotoHome()
        {
            navigationService.NavigateToViewModel<MyWeekViewModel>();
        }

        protected void GotoNews()
        {
            navigationService.NavigateToViewModel<PeopleViewModel>();
        }

        protected async void ChooseWorkout(ItemClickEventArgs evt)
        {
            var item = evt.ClickedItem as WorkoutDetailViewModel;

            if (item.TrainingType == "RUN")
            {
                navigationService.UriFor<RunViewModel>().WithParam(x => x.Slug, item.Slug).Navigate();
            }
            else
            {
                SetUp = true;
                LoadAlternative = true;
                ChooseAlternative = false;
                SelectedWorkout = item;

                try
                {
                    WorkoutVariants = await dataservice.LoadAlternatives(item.Name);
                    LoadAlternative = false;
                    ChooseAlternative = true;
                    SelectedWorkout = WorkoutVariants.Where(w => w.Slug == item.Slug).First();
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        protected async void LetsGo()
        {
            var cache = await dataservice.GetAvailableWorkouts();
            if (!cache.Any(w => w.Slug == SelectedWorkout.Slug))
            {
                SelectedWorkout.TrainingType = "INTERNAL";
                cache.Add(SelectedWorkout);
            }

            navigationService.UriFor<WorkoutDetailViewModel>()
                    .WithParam(x => x.TrainingId, -1)
                    .WithParam(x => x.Slug, SelectedWorkout.Slug)
                    .Navigate();
        }
    }
}
