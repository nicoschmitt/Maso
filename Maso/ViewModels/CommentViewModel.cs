using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class CommentViewModel : PropertyChangedBase
    {
        public string Id { get; set; }
        public string User { get; set; }
        public string Avatar { get; set; }
        public string When { get; set; }
        public string Description { get; set; }

        public CommentViewModel()
        {
#if DEBUG
            if (Execute.InDesignMode)
            {
                When = "a month ago";
                User = "Julian Pimpi";
                Avatar = "https://freeletics-storage.s3.amazonaws.com/small/11c088a6817bdffe221c6988f11f86acae65bcc2.jpg";
                Description = "Nice!";
            }
#endif
        }
    }
}
