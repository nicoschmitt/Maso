using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Maso.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;
        private string versionName;

        public AboutViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;

            var pv = Package.Current.Id.Version;
            Version version = new Version(Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Version.Revision);

            VersionName = "Maso v" + version.ToString();
        }

        protected void GotoHome()
        {
            navigationService.NavigateToViewModel<MyWeekViewModel>();
        }
        
        public string VersionName
        {
            get { return versionName; }
            set
            {
                versionName = value;
                this.NotifyOfPropertyChange(() => VersionName);
            }
        }

        public async void FollowMe()
        {
            try
            {
                await dataservice.Follow("4990787");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
