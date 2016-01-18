using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class PeopleViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;
        private bool loading, display;

        public BindableCollection<FeedViewModel> Feed
        {
            get;
            private set;
        }

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

        public PeopleViewModel() : this(null, null) { }

        public PeopleViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;

            Loading = true;
            Display = false;
            Feed = new BindableCollection<FeedViewModel>();
        }

        protected async override void OnActivate()
        {
            base.OnActivate();
            try
            {
                var feed = await dataservice.GetPeopleNews();
                Feed.AddRange(feed);
            }
            catch (Exception ex)
            {
                dataservice.LogException(ex);
            }

            Loading = false;
            Display = true;
        }

        protected async void RefreshData()
        {
            var feed = await dataservice.GetPeopleNews();
            Feed.Clear();
            Feed.AddRange(feed);
            Loading = false;
            Display = true;
        }

        protected void GotoHome()
        {
            navigationService.NavigateToViewModel<MyWeekViewModel>();
        }
        
        protected void GotoFreeTraining()
        {
            navigationService.NavigateToViewModel<FreeTrainingViewModel>();
        }

    }
}
