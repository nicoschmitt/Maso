using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class MediaPlayerViewModel : ViewModelBase
    {
        private string media;
        public string Media
        {
            get { return media; }
            set
            {
                media = value;
                this.NotifyOfPropertyChange(() => Media);
            }
        }

        public MediaPlayerViewModel() : this(null) { }

        public MediaPlayerViewModel(INavigationService navigationService) : base(navigationService) { }
    }
}
