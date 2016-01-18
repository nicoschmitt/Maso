using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.Models
{
    [JsonObject("training")]
    public class Completion
    {
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("seconds")]
        public int Seconds { get; set; }
        [JsonProperty("star")]
        public bool Star { get; set; }
        [JsonProperty("performed_at")]
        public DateTime PerformedAt { get; set; }
        [JsonProperty("exercises_seconds")]
        public int[][] ExercisesSeconds { get; set; }
    }

    [JsonObject("training")]
    public class CoachCompletion : Completion
    {
        [JsonProperty("exertion_preference")]
        public int Preference { get; set; }

        [JsonProperty("exertion_rate")]
        public int Rate { get; set; }

    }
}
