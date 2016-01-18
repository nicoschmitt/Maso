using Caliburn.Micro;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.ViewModels
{
    public class ExercisesViewModel : PropertyChangedBase
    {
        private string slug;
        [JsonProperty]
        public string Slug
        {
            get { return slug; }
            set
            {
                slug = value;
                this.NotifyOfPropertyChange(() => Slug);
            }
        }

        private string title;
        [JsonProperty]
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                this.NotifyOfPropertyChange(() => Title);
            }
        }

        private string image;
        [JsonProperty]
        public string Image
        {
            get { return image; }
            set
            {
                image = value;
                this.NotifyOfPropertyChange(() => Image);
            }
        }

        private string video;
        [JsonProperty]
        public string Video
        {
            get { return video; }
            set
            {
                video = value;
                this.NotifyOfPropertyChange(() => Video);
            }
        }

        [JsonProperty]
        public int Quantity { get; set; }

        private string repetition;
        [JsonProperty]
        public string Repetition
        {
            get { return repetition; }
            set
            {
                repetition = value;
                this.NotifyOfPropertyChange(() => Repetition);
            }
        }

        private int seconds;
        [JsonProperty]
        public int Seconds
        {
            get { return seconds; }
            set
            {
                seconds = value;
                this.NotifyOfPropertyChange(() => Seconds);
            }
        }

        public int BestSeconds { get; set; }

        public ExercisesViewModel()
        {
#if DEBUG
            if (Execute.InDesignMode)
            {
                Slug = "burpees";
                Title = "10X Burpees";
                Image = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-96x54.jpg";
                Video = "https://d2t4u40hiq4b70.cloudfront.net/videos/v2/burpees-fr.mp4";
            }
#endif
        }

        public ExercisesViewModel Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ExercisesViewModel>(json);
        }
    }

    public class ExerciesSeparatorViewModel : ExercisesViewModel { }
}
