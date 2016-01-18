using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class FreeTrainingViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        
        public FreeTrainingViewModel() : this(null) { }

        public FreeTrainingViewModel(INavigationService navigationService) : base(navigationService)
        {
            this.navigationService = navigationService;   
        }
        
        protected void DoWorkouts()
        {
            navigationService.UriFor<FreeWorkoutViewModel>().WithParam(v => v.TrainingType, "WORKOUTS").Navigate();
        }

        protected void DoExercices()
        {
            navigationService.UriFor<FreeWorkoutViewModel>().WithParam(v => v.TrainingType, "EXERCICES").Navigate();
        }

        protected void DoRun()
        {
            navigationService.UriFor<FreeWorkoutViewModel>().WithParam(v => v.TrainingType, "RUN").Navigate();
        }

        protected void GotoHome()
        {
            navigationService.NavigateToViewModel<MyWeekViewModel>();
        }

        protected void GotoNews()
        {
            navigationService.NavigateToViewModel<PeopleViewModel>();
        }
    }
}
