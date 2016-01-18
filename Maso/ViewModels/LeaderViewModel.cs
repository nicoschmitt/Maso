using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class LeaderViewModel : PropertyChangedBase
    {
        private string title;
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                this.NotifyOfPropertyChange(() => Title);
            }
        }

        private int level;
        public int Level
        {
            get { return level; }
            set
            {
                level = value;
                this.NotifyOfPropertyChange(() => Level);
            }
        }

        private string avatar;
        public string Avatar
        {
            get { return avatar; }
            set
            {
                avatar = value;
                this.NotifyOfPropertyChange(() => Avatar);
            }
        }

        private string time;
        public string Time
        {
            get { return time; }
            set
            {
                time = value;
                this.NotifyOfPropertyChange(() => Time);
            }
        }

        private bool pb;
        public bool PB
        {
            get { return pb; }
            set
            {
                pb = value;
                this.NotifyOfPropertyChange(() => PB);
            }
        }

        private bool star;
        public bool Star
        {
            get { return star; }
            set
            {
                star = value;
                this.NotifyOfPropertyChange(() => Star);
            }
        }

        public string When { get; set; }

        public LeaderViewModel()
        {
#if DEBUG
            if (Execute.InDesignMode)
            {
                Title = "Julian Pimpi";
                Level = 50;
                Avatar = "https://freeletics-storage.s3.amazonaws.com/small/ea4bf7f02695657c83a30bda25e3e2a337c90173.jpg";
                Star = true;
                PB = true;
                Time = "6:03";
                When = "3 hours ago";
            }
#endif
        }
    }
}
