using Caliburn.Micro;
using Maso.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class FeedViewModel : ViewModelBase
    {
        private int clapCount;
        private bool commentLoaded;
        private string newComment;

        public BindableCollection<CommentViewModel> Comments
        {
            get;
            private set;
        }
        
        public string Id { get; set; }
        public string User { get; set; }
        public string Avatar { get; set; }
        public string Workout { get; set; }
        public string Variant { get; set; }
        public string Image { get; set; }
        public bool HasImage
        {
            get { return !string.IsNullOrWhiteSpace(Image); }
        }
        public bool PB { get; set; }
        public bool Star { get; set; }
        public string Time { get; set; }
        public string When { get; set; }
        public string Description { get; set; }
        public bool HasDescription
        {
            get { return !string.IsNullOrWhiteSpace(Description); }
        }
        public bool HasClapped { get; set; }
        public int CommentCount { get; set; }
        public bool CommentLoaded
        {
            get { return commentLoaded; }
            set
            {
                commentLoaded = value;
                this.NotifyOfPropertyChange(() => CommentLoaded);
            }
        }
        public string NewComment
        {
            get { return newComment; }
            set
            {
                newComment = value;
                this.NotifyOfPropertyChange(() => NewComment);
            }
        }
        public int ClapCount
        {
            get { return clapCount; }
            set
            {
                clapCount = value;
                this.NotifyOfPropertyChange(() => ClapCount);
            }
        }

        public string TimeIcon
        {
            get
            {
                return Time == null ? "" : Freeletics.GetTimeIcon(PB, Star, true);
            }
        }

        public string ClapImage
        {
            get
            {
                return HasClapped ? "ms-appx:///Assets/clap.png" : "ms-appx:///Assets/clap-no.png";
            }
        }

        public FeedViewModel() : this(null) { }

        public FeedViewModel(INavigationService navigation) : base(navigation)
        {
            Comments = new BindableCollection<CommentViewModel>();

#if DEBUG
            if (Execute.InDesignMode)
            {
                Star = PB = true;
                Time = "3:04";
                When = "a month ago";
                User = "Julian Pimpi";
                Avatar = "https://freeletics-storage.s3.amazonaws.com/small/11c088a6817bdffe221c6988f11f86acae65bcc2.jpg";
                Workout = "ARES";
                Variant = "standard";
                ClapCount = 5;
                HasClapped = false;
                CommentCount = 5;
                CommentLoaded = true;
                Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum";
                Image = "https://freeletics-storage.s3.amazonaws.com/trainings/feed/482b684b2039b854ff4704d554a32ec7f495bf14.jpg";
            }
#endif
        }

        protected async void OnClap()
        {
            var dataService = IoC.Get<IFreeletics>();
            await dataService.Clap(this.Id, !HasClapped);

            HasClapped = !HasClapped;
            ClapCount++;
            this.NotifyOfPropertyChange(() => ClapImage);
        }

        protected async void OnShowComments(bool force = false)
        {
            if (!CommentLoaded || force)
            {
                CommentLoaded = true;
                var dataService = IoC.Get<IFreeletics>();
                Comments.Clear();
                Comments.AddRange(await dataService.GetComments(this.Id));
            }
        }

        protected async void PostComment()
        {
            try
            {
                var dataService = IoC.Get<IFreeletics>();
                await dataService.PostComment(this.Id, NewComment);
                NewComment = "";
                OnShowComments(true);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
