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
    public class WorkoutDetailViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;

        private string slug, title, variant;
        private bool isSwitchable;

        public BindableCollection<ExercisesViewModel> Exercises
        {
            get;
            private set;
        }

        public BindableCollection<LeaderViewModel> Leaders
        {
            get;
            private set;
        }

        public int TrainingId { get; set; }
        public int WorkoutId { get; set; }
        public string TrainingType { get; set; }
        public string Name { get; set; }

        public double Points { get; set; }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                this.NotifyOfPropertyChange(() => Title);
            }
        }

        public string Slug
        {
            get { return slug; }
            set
            {
                slug = value;
                this.NotifyOfPropertyChange(() => Slug);
            }
        }

        public string Variant
        {
            get { return variant; }
            set
            {
                variant = value;
                this.NotifyOfPropertyChange(() => Variant);
            }
        }

        public string FullTitle
        {
            get
            {
                return Title + " " + Variant;
            }
        }

        public bool IsSwitchable
        {
            get { return isSwitchable; }
            set
            {
                isSwitchable = value;
                this.NotifyOfPropertyChange(() => IsSwitchable);
            }
        }

        public WorkoutDetailViewModel() : this(null, null) { }

        public WorkoutDetailViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;

            Exercises = new BindableCollection<ExercisesViewModel>();
            Leaders = new BindableCollection<LeaderViewModel>();

#if DEBUG
            if (Execute.InDesignMode)
            {
                Slug = "dione-standard-3";
                Title = "2X METIS";
                IsSwitchable = false;
                Exercises.Add(new ExercisesViewModel() { Title = "Burpees", Image = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-96x54.jpg" });
                Exercises.Add(new ExercisesViewModel() { Title = "Burpees", Image = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-96x54.jpg" });
                Exercises.Add(new ExercisesViewModel() { Title = "Burpees", Image = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-96x54.jpg" });
            }
#endif

            if (dataservice != null)
            {
                Exercises.Add(new ExerciesSeparatorViewModel() { Title = "loading..." });
            }
        }

        public void ShowMedia(ItemClickEventArgs evt)
        {
            var item = evt.ClickedItem as ExercisesViewModel;
            if (item != null && !string.IsNullOrWhiteSpace(item.Video))
            {
                navigationService.UriFor<MediaPlayerViewModel>().WithParam(x => x.Media, item.Video).Navigate();
            }
        }

        public bool CanLetsGo(string slug)
        {
            return !string.IsNullOrWhiteSpace(this.Slug);
        }

        public void LetsGo(string slug)
        {
            navigationService.UriFor<LetsGoViewModel>()
                .WithParam(x => x.TrainingId, this.TrainingId)
                .WithParam(x => x.WorkoutId, this.WorkoutId)
                .WithParam(x => x.Slug, this.Slug)
                .Navigate();
        }

        protected async override void OnActivate()
        {
            base.OnActivate();
            
            if (!Execute.InDesignMode && dataservice != null)
            {
                if (TrainingId >= 0)
                {
                    // Coach

                    var week = await dataservice.GetMyWeekFromCache();
                    var training = week.Trainings.First(x => x.Id == TrainingId);
                    IsSwitchable = training.Switchable;

                    var workout = training.Workouts.First(w => w.Id == WorkoutId);
                    this.Title = workout.Title;
                    this.Slug = workout.Slug;

                    await GetDetails();
                }
                else
                {
                    // Free Training

                    var workouts = await dataservice.GetAvailableWorkouts();
                    var workout = workouts.First(w => w.Slug == this.Slug);
                    this.Title = workout.FullTitle;

                    this.Exercises.Clear();
                    this.Exercises.AddRange(workout.Exercises);

                    try
                    {
                        var leaderboard = await dataservice.GetLeaderboard(this.Slug);
                        this.Leaders.AddRange(leaderboard);
                    }
                    catch (Exception ex)
                    {
                        dataservice.LogException(ex);
                    }
                }
            }
        }

        private async Task GetDetails()
        {
            // Exercices
            try
            {
                var details = await dataservice.GetWorkoutDetail(this.Slug);

                this.Exercises.Clear();
                this.Title = details.Title;
                this.Exercises.AddRange(details.Exercises);

                // Leaderboard

                var leaderboard = await dataservice.GetLeaderboard(this.Slug);
                this.Leaders.AddRange(leaderboard);
            }
            catch (Exception ex)
            {
                dataservice.LogException(ex, this.Slug);
                ShowError(ex);
            }
        }

        protected async void Switch()
        {
            await dataservice.Switch(this.TrainingId);
            await dataservice.GetMyWeek();
            navigationService.NavigateToViewModel<MyWeekViewModel>();
        }
    }
}
