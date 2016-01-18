using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Maso.ViewModels
{
    public class TrainingViewModel : PropertyChangedBase
    {
        public BindableCollection<WorkoutViewModel> Workouts { get; set; }
        public int Id { get; set; }
        public bool Switchable { get; set; }

        public TrainingViewModel()
        {
            Workouts = new BindableCollection<WorkoutViewModel>();
        }
        
        public void WorkoutClick(ItemClickEventArgs args)
        {
            var workout = args.ClickedItem as WorkoutViewModel;

            var navigationService = IoC.Get<INavigationService>();
            navigationService.UriFor<WorkoutDetailViewModel>()
                .WithParam(x => x.TrainingId, Id)
                .WithParam(x => x.WorkoutId, workout.Id)
                .Navigate();
        }
    }
}
