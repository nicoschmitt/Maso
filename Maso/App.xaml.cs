using Caliburn.Micro;
using Maso.Models;
using Maso.ViewModels;
using Maso.Views;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;

namespace Maso
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App
    { 
        private WinRTContainer container;
        private INavigationService navigationService;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            WindowsAppInitializer.InitializeAsync();
          
            this.InitializeComponent();
        }

        protected override void Configure()
        {
            container = new WinRTContainer();
            container.RegisterWinRTServices();

            container.Singleton<IFreeletics, Freeletics>();
            container.Singleton<ISpeechService, SpeechService>();

            container
                .PerRequest<LoadingViewModel>()
                .PerRequest<MyWeekViewModel>()
                .PerRequest<PeopleViewModel>()
                .PerRequest<WorkoutViewModel>()
                .PerRequest<TrainingViewModel>()
                .PerRequest<WorkoutDetailViewModel>()
                .PerRequest<ExercisesViewModel>()
                .PerRequest<ExerciesSeparatorViewModel>()
                .PerRequest<LeaderViewModel>()
                .PerRequest<LetsGoViewModel>()
                .PerRequest<AboutViewModel>()
                .PerRequest<FreeTrainingViewModel>()
                .PerRequest<FreeWorkoutViewModel>()
                .PerRequest<RunViewModel>()
                .PerRequest<MediaPlayerViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = container.GetInstance(service, key);
            if (instance != null) return instance;
            throw new Exception("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void PrepareViewFirst(Frame rootFrame)
        {
            navigationService = container.RegisterNavigationService(rootFrame);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Initialize();

            var resumed = false;
            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                try
                {
                    resumed = navigationService.ResumeState();
                }
                catch
                {
                    resumed = false;
                }
            }

            if (!resumed)
            {
                var speech = IoC.Get<ISpeechService>();
                speech.InitializeVoiceCommands();

                DisplayRootView<LoadingView>();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            
            if (args.Kind == ActivationKind.PickFileContinuation)
            {
                OnContinueFileOpenPicker((IFileOpenPickerContinuationEventArgs)args);
            }
            else if (args.Kind == ActivationKind.VoiceCommand)
            {
                var vcArgs = (VoiceCommandActivatedEventArgs)args;
                var command = vcArgs.Result.RulePath.FirstOrDefault();
                command.ToString();
            }
        }

        private void OnContinueFileOpenPicker(IFileOpenPickerContinuationEventArgs args)
        {
            if (args.Files == null || !args.Files.Any()) return;

            var eventAggregator = container.GetInstance<IEventAggregator>();
            eventAggregator.PublishOnUIThread(args);
        }

        protected override void OnSuspending(object sender, SuspendingEventArgs e)
        {
            base.OnSuspending(sender, e);
            navigationService.SuspendState();
        }
    }
}