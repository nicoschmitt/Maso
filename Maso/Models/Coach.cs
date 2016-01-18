using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maso.Models
{
    public class Coach
    {
        public Coach()
        {
            HealthLimitation = new string[0];
            SessionsNumber = 4;
        }

        [JsonProperty("coach_focus")]
        public string Focus { get; set; }
        
        [JsonProperty("sessions_number")]
        public int SessionsNumber { get; set; }

        [JsonProperty("health_limitations")]
        public string[] HealthLimitation { get; set; }
    }
}
