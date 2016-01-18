using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Maso.ViewModels
{
    public class LetsGoViewModel : ViewModelBase, IHandle<IFileOpenPickerContinuationEventArgs>
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;
        private WorkoutDetailViewModel workout;
        private DispatcherTimer timer;
        private Windows.System.Display.DisplayRequest displayRequest;
        private ExercisesViewModel[] exos;

        private string slug, chrono, image, description, bestTime, pbVariation;
        private DateTime startedAt, finishedAt, startRest;
        private bool finished, working, star, canSendData, countdown, isCoach;
        private bool coachWasEasy, coachWasOk, coachWasHard;
        private ExercisesViewModel current;
        private string gotext;
        private int idx = -1, howhard;
        private DateTime currentTimeStart;

        public int TrainingId { get; set; }

        public int WorkoutId { get; set; }

        public string Slug
        {
            get { return slug; }
            set
            {
                slug = value;
                this.NotifyOfPropertyChange(() => Slug);
            }
        }

        public int HowHard
        {
            get { return howhard; }
            set
            {
                howhard = value;
                this.NotifyOfPropertyChange(() => HowHard);
            }
        }
        
        public bool CoachTooEasy
        {
            get { return coachWasEasy; }
            set
            {
                coachWasEasy = value;
                coachWasHard = coachWasOk = !value;
                this.NotifyOfPropertyChange(() => CoachTooEasy);
                this.NotifyOfPropertyChange(() => CoachOk);
                this.NotifyOfPropertyChange(() => CoachTooHard);
            }
        }

        public bool CoachOk
        {
            get { return coachWasOk; }
            set
            {
                coachWasOk = value;
                coachWasHard = coachWasEasy = !value;
                this.NotifyOfPropertyChange(() => CoachTooEasy);
                this.NotifyOfPropertyChange(() => CoachOk);
                this.NotifyOfPropertyChange(() => CoachTooHard);
            }
        }

        public bool CoachTooHard
        {
            get { return coachWasHard; }
            set
            {
                coachWasHard = value;
                coachWasOk = coachWasEasy = !value;
                this.NotifyOfPropertyChange(() => CoachTooEasy);
                this.NotifyOfPropertyChange(() => CoachOk);
                this.NotifyOfPropertyChange(() => CoachTooHard);
            }
        }

        public string GoText
        {
            get { return gotext; }
            set
            {
                gotext = value;
                this.NotifyOfPropertyChange(() => GoText);
            }
        }

        public string Chrono
        {
            get { return chrono; }
            set
            {
                chrono = value;
                this.NotifyOfPropertyChange(() => Chrono);
            }
        }

        public bool HasPB
        {
            get { return !string.IsNullOrEmpty(BestTime); }
        }

        public string BestTime
        {
            get { return bestTime; }
            set
            {
                bestTime = value;
                this.NotifyOfPropertyChange(() => HasPB);
                this.NotifyOfPropertyChange(() => BestTime);
            }
        }

        public string PBVariation
        {
            get { return pbVariation; }
            set
            {
                pbVariation = value;
                this.NotifyOfPropertyChange(() => PBVariation);
            }
        }

        public string Image
        {
            get { return image; }
            set
            {
                image = value;
                this.NotifyOfPropertyChange(() => Image);
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                this.NotifyOfPropertyChange(() => Description);
            }
        }

        public bool Star
        {
            get { return star; }
            set
            {
                star = value;
                this.NotifyOfPropertyChange(() => Star);
                this.NotifyOfPropertyChange(() => StarImage);
            }
        }

        public string StarImage
        {
            get { return Star ? "ms-appx:///Assets/star-yes.png" : "ms-appx:///Assets/star-no.png"; }
        }

        public bool Finished
        {
            get { return finished; }
            set
            {
                finished = value;
                this.NotifyOfPropertyChange(() => Finished);
            }
        }

        public bool Working
        {
            get { return working; }
            set
            {
                working = value;
                this.NotifyOfPropertyChange(() => Working);
            }
        }

        public bool CanSendData
        {
            get { return canSendData; }
            set
            {
                canSendData = value;
                this.NotifyOfPropertyChange(() => CanSendData);
            }
        }

        public bool IsCoach
        {
            get { return isCoach; }
            set
            {
                isCoach = value;
                this.NotifyOfPropertyChange(() => IsCoach);
            }
        }

        public ExercisesViewModel CurrentExercise
        {
            get { return current; }
            set
            {
                current = value;
                this.NotifyOfPropertyChange(() => CurrentExercise);
            }
        }

        public LetsGoViewModel() : this(null, null, null) { }

        public LetsGoViewModel(INavigationService navigationService, IFreeletics dataservice, IEventAggregator events) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;
            if (events != null) events.Subscribe(this);

            Chrono = "5";
            countdown = true;
            Working = true;
            Finished = false;
            Star = true;
            CanSendData = true;

#if DEBUG
            if (Execute.InDesignMode)
            {
                CurrentExercise = new ExercisesViewModel() { Slug = "burpees", Title = "10X Burpees", Image = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-96x54.jpg" };

                Working = false;
                Finished = true;
                BestTime = "PB: 03:10";
                PBVariation = "(-3)";
                IsCoach = true;

                GoText = "Go";
                Slug = "kentauros-endurance-6";
                var fix = Slug.Replace("endurance", "medium");
                fix = fix.Replace("strengh", "medium");
                Image = string.Format("https://www.freeletics.com/images/workout/{0}.png", fix);
            }
#endif
        }

        protected async override void OnActivate()
        {
            base.OnActivate();

            if (navigationService != null)
            {
                navigationService.BackPressed += BackNavPressed;
            }

            if (dataservice != null)
            {
                GoText = "Go";

                var fix = Slug.Replace("endurance", "medium");
                fix = fix.Replace("strength", "medium");
                fix = fix.Replace("standard", "medium");
                fix = fix.Replace("-home", "");

                Image = string.Format("https://www.freeletics.com/images/workout/{0}.png", fix);

                if (TrainingId >= 0)
                {
                    // Coach
                    workout = dataservice.GetWorkoutFromCache(Slug);
                    IsCoach = true;
                }
                else
                {
                    // Free Training
                    var available = await dataservice.GetAvailableWorkouts();
                    workout = available.First(w => w.Slug == this.Slug);
                    IsCoach = false;
                }

                exos = workout.Exercises.Where(w => w.Slug != null).ToArray();
                
                CurrentExercise = exos.First();

                try
                {
                    var data = await dataservice.GetBestData(Slug);
                    if (data != null)
                    {
                        var performed = TimeSpan.FromSeconds(data.Seconds);
                        if (performed.TotalHours >= 1)
                        {
                            BestTime = "PB: " + performed.ToString(@"hh\:mm\:ss");
                        }
                        else
                        {
                            BestTime = "PB: " + performed.ToString(@"mm\:ss");
                        }

                        if (exos.Length == 1)
                        {
                            exos[0].BestSeconds = data.Seconds;
                        }
                        else if (data.ExercisesSeconds != null)
                        {
                            try
                            {
                                int i = 0, sec = 0;
                                foreach (var t in data.ExercisesSeconds)
                                {
                                    foreach (var w in t)
                                    {
                                        sec += w;
                                        exos[i++].BestSeconds = sec;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dataservice.LogException(ex);
                }
            }
        }

        protected void OnStar()
        {
            Star = !Star;
        }

        private async void BackNavPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            e.Handled = true;

            var dialog = new MessageDialog("Are you sure you want to cancel workout?");
            dialog.Commands.Add(new UICommand("Yes"));
            dialog.Commands.Add(new UICommand("No"));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();
            if (result.Label.Equals("Yes"))
            {
                if (timer != null) timer.Stop();
                try { if (displayRequest != null) displayRequest.RequestRelease(); } catch { }

                navigationService.BackPressed -= BackNavPressed;
                navigationService.GoBack();
            }
        }

        private void Tick(object sender, object e)
        {
            if (countdown)
            {
                var elapsed = DateTime.Now.Subtract(startedAt);
                if (elapsed.TotalSeconds >= 5)
                {
                    countdown = false;
                    Chrono = "00:00";
                    startedAt = currentTimeStart = DateTime.Now;
                }
                else
                {
                    Chrono = (5 - elapsed.TotalSeconds).ToString("0");
                }
            }
            else if (CurrentExercise.Slug != "rest")
            {
                var elapsed = DateTime.Now.Subtract(startedAt);
                if (elapsed.TotalHours >= 1)
                {
                    Chrono = elapsed.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    Chrono = elapsed.ToString(@"mm\:ss");
                }
            }
            else
            {
                var elapsed = DateTime.Now.Subtract(startRest).TotalSeconds;
                var remaining = CurrentExercise.Quantity - elapsed;
                if (remaining <= 0)
                {
                    Chrono = "0";
                    LetsGo(false);
                }
                else
                {
                    Chrono = remaining.ToString("0");
                }
            }
        }

        protected bool CanLetsGo(bool finished)
        {
            return !Finished;
        }

        protected void LetsGo(bool finished)
        {
            if (idx < 0)
            {
                idx = 0;

                startedAt = DateTime.Now;
                displayRequest = new Windows.System.Display.DisplayRequest();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Tick;
                timer.Start();

                displayRequest.RequestActive();
            }
            else if (countdown)
            {
                countdown = false;
                Chrono = "00:00";
                startedAt = currentTimeStart = DateTime.Now;
                return;
            }
            else
            {
                CurrentExercise.Seconds = (int)DateTime.Now.Subtract(currentTimeStart).TotalSeconds;
                currentTimeStart = DateTime.Now;

                if (HasPB)
                {
                    var elapsed = (int)DateTime.Now.Subtract(startedAt).TotalSeconds;
                    var progress = elapsed - CurrentExercise.BestSeconds;
                    PBVariation = (progress < 0 ? "(-" : "(+") + TimeSpan.FromSeconds(progress).ToString(@"mm\:ss") + ")";
                }
            }

            if (idx < exos.Length - 1)
            {
                var next = new string[] { "Ok, next!", "Bring it!", "That did not hurt!", "I want more", "I'm still breathing!" };
                GoText = next[new Random().Next(next.Length)];
                CurrentExercise = exos[idx++];
            }
            else if (idx == exos.Length - 1)
            {
                GoText = "FINISH";
                CurrentExercise = exos[idx++];
            }
            else
            {
                Finished = true;
                Working = false;
                timer.Stop();
                try { displayRequest.RequestRelease(); } catch { }

                finishedAt = DateTime.Now;
            }

            if (CurrentExercise.Slug == "rest")
            {
                startRest = DateTime.Now;
            }
        }

        protected async void SendData()
        {
            CanSendData = false;

            var elapsed = (int)finishedAt.Subtract(startedAt).TotalSeconds;
            Completion completion = new Completion();
            if (IsCoach && (CoachOk || CoachTooEasy || CoachTooHard))
            {
                completion = new CoachCompletion() { Rate = HowHard, Preference = (CoachOk ? 0 : (CoachTooEasy ? 1 : -1)) };
            }

            completion.Slug = this.Slug;
            completion.Seconds = elapsed;
            completion.Description = Description;
            completion.Star = star;
            completion.PerformedAt = DateTime.Now.ToUniversalTime();

            var detail = new List<List<int>>();
            List<int> current = new List<int>();
            foreach (var ex in workout.Exercises)
            {
                if (ex.Slug == null)
                {
                    current = new List<int>();
                    detail.Add(current);
                }
                else
                {
                    current.Add(ex.Seconds);
                }
            }
            completion.ExercisesSeconds = detail.Select(l => l.ToArray()).ToArray();

            try
            {
                try
                {
                    string sessionid = await dataservice.SendCompletion(completion);
                }
                catch (SendLaterException ex)
                {
                    ShowError(ex);
                }

                if (TrainingId >= 0)
                {
                    await dataservice.GetMyWeek();
                }

                if (navigationService != null)
                {
                    navigationService.BackPressed -= BackNavPressed;
                }
                navigationService.NavigateToViewModel<MyWeekViewModel>();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            CanSendData = true;
        }

        protected void AttachPhoto()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                picker.FileTypeFilter.Clear();
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.PickSingleFileAndContinue();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void Handle(IFileOpenPickerContinuationEventArgs message)
        {
            var file = message.Files.First();
            
        }
    }
}
