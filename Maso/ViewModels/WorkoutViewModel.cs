using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Maso.ViewModels
{
    public class WorkoutViewModel : PropertyChangedBase
    {
        private int id, idxWeek;
        private bool active;
        private string title;
        private string workoutType;
        private string name;
        private string slug;
        private string time;

        public WorkoutViewModel()
        {
#if DEBUG
            if (Execute.InDesignMode)
            {
                IdxWeek = 1;
                Name = "gaia";
                Title = "4/6 GAIA";
                WorkoutType = "Standard";
                Active = false;
                Slug = "dione-standard-3";
                Time = "3:04";
                When = "3 days ago";
            }
#endif
        }

        public string Number
        {
            get
            {
                if (string.IsNullOrWhiteSpace(time)) return IdxWeek.ToString();
                else return "✓";
            }
        }

        public int Id
        {
            get { return id; }
            set
            {
                id = value;
                this.NotifyOfPropertyChange(() => Id);
            }
        }
        
        public int IdxWeek
        {
            get { return idxWeek; }
            set
            {
                idxWeek = value;
                this.NotifyOfPropertyChange(() => IdxWeek);
            }
        }

        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                this.NotifyOfPropertyChange(() => Active);
                this.NotifyOfPropertyChange(() => Background);
            }
        }

        public Color Background
        {
            get
            {
                if (active)
                {
                    return Color.FromArgb(255, 32, 132, 240);
                }
                else if (!string.IsNullOrWhiteSpace(Time))
                {
                    return Color.FromArgb(255, 128, 128, 128);
                }
                else
                {
                    return Color.FromArgb(255, 128, 128, 128);
                }
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                this.NotifyOfPropertyChange(() => Name);
            }
        }

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

        public string WorkoutType
        {
            get { return workoutType; }
            set
            {
                workoutType = value;
                this.NotifyOfPropertyChange(() => WorkoutType);
            }
        }

        public string Time
        {
            get { return time; }
            set
            {
                time = value;
                this.NotifyOfPropertyChange(() => Time);
            }
        }

        public string When { get; set; }

        public bool PB { get; set; }

        public bool Star { get; set; }

        public string TimeIcon
        {
            get
            {
                return time == null ? "" : Freeletics.GetTimeIcon(PB, Star);
            }
        }
    }
}
