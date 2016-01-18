using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class LoadingViewModel : ViewModelBase
    {
        INavigationService navigationService;
        private IFreeletics service;
        private bool needLogin;
        private bool isLoading;
        private string username, password;
        
        public bool NeedLogin
        {
            get { return needLogin; }
            set
            {
                needLogin = value;
                this.NotifyOfPropertyChange(() => NeedLogin);
            }
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                this.NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public string UserName
        {
            get { return username; }
            set
            {
                username = value;
                this.NotifyOfPropertyChange(() => UserName);
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                this.NotifyOfPropertyChange(() => Password);
            }
        }

        public LoadingViewModel() : this(null, null) { }

        public LoadingViewModel(INavigationService navigationService, IFreeletics service) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.service = service;

#if DEBUG
            if (Execute.InDesignMode)
            {
                NeedLogin = true;
                IsLoading = true;
            }
#endif
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            navigationService.BackStack.Clear();

            if (service != null)
            {
                if (service.HasLoginInfo())
                {
                    DoLogin(null, null);
                }
                else
                {
                    NeedLogin = true;
                    IsLoading = false;
                }
            }
        }

        protected bool CanDoLogin(string username, string password)
        {
            return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrEmpty(Password);
        }

        protected async void DoLogin(string username, string password)
        {
            NeedLogin = false;
            IsLoading = true;

            bool sucess = await service.Login(UserName, Password);
            if (sucess)
            {
                GetDataAndDisplay();
            }
            else
            {
                IsLoading = false;
                NeedLogin = true;
            }
        }

        private async void GetDataAndDisplay()
        {
            var myweek = await service.GetMyWeek();
            if (myweek != null)
            {
                navigationService.NavigateToViewModel<MyWeekViewModel>();
            } else
            {
                navigationService.NavigateToViewModel<PeopleViewModel>();
            }
        }
    }
}
