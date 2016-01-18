using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class RunViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IFreeletics dataservice;

        private DateTimeOffset date;
        private TimeSpan time;
        private string duration, error, description;

        public string Slug { get; set; }

        public DateTimeOffset Date
        {
            get { return date; }
            set
            {
                date = value;
                this.NotifyOfPropertyChange(() => Date);
            }
        }

        public TimeSpan Time
        {
            get { return time; }
            set
            {
                time = value;
                this.NotifyOfPropertyChange(() => Time);
            }
        }

        public string Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                this.NotifyOfPropertyChange(() => Duration);
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
        
        public string ErrorMessage
        {
            get { return error; }
            set
            {
                error = value;
                this.NotifyOfPropertyChange(() => ErrorMessage);
            }
        }

        public RunViewModel() : this(null, null) { }

        public RunViewModel(INavigationService navigationService, IFreeletics dataservice) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.dataservice = dataservice;

            Date = new DateTimeOffset(DateTime.Now);
            Time = DateTime.Now - DateTime.Today;
            Duration = "00:00";
        }
        
        public async void SendData()
        {
            ErrorMessage = "";
            try
            {
                var elapsed = ParseTimeSpan(Duration);
                var fulldate = new DateTime(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds);
                var completion = new Completion()
                {
                    Slug = Slug,
                    Star = false,
                    Seconds = (int)elapsed.TotalSeconds,
                    Description = Description,
                    PerformedAt = fulldate.ToUniversalTime()
                };

                try
                {
                    await dataservice.SendCompletion(completion);
                    navigationService.NavigateToViewModel<MyWeekViewModel>();
                }
                catch(SendLaterException ex)
                {
                    ShowError(ex);
                    navigationService.NavigateToViewModel<MyWeekViewModel>();
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                    dataservice.LogException(ex, completion);
                }
            }
            catch (FormatException)
            {
                ErrorMessage = "Wrong input format. Please use hh:mm:ss";
            }
        }

        public static TimeSpan ParseTimeSpan(string s)
        {
            if (Regex.IsMatch(s, @"\d+\:\d+\:\d+"))
            {
                return TimeSpan.ParseExact(s, @"hh\:mm\:ss", null);
            }
            else if (Regex.IsMatch(s, @"\d+\:\d+"))
            {
                return TimeSpan.ParseExact(s, @"mm\:ss", null);
            }
            else
            {
                return TimeSpan.Parse(s);
            }
        }
    }
}
