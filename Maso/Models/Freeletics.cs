using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Maso.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Windows.ApplicationModel;

namespace Maso.Models
{
    public interface IFreeletics
    {
        bool HasLoginInfo();
        Task<bool> Login(string username, string password);
        void Disconnect();
        Task<MyWeekViewModel> GetMyWeek();
        Task<MyWeekViewModel> GetMyWeekFromCache();
        Task<WorkoutDetailViewModel> GetWorkoutDetail(string slug);
        WorkoutDetailViewModel GetWorkoutFromCache(string slug);
        Task<IList<LeaderViewModel>> GetLeaderboard(string slug);
        Task<string> SendCompletion(Completion completion);
        Task AttachPhoto(string trainingid, StorageFile file);
        Task<IEnumerable<FeedViewModel>> GetPeopleNews();
        Task<IEnumerable<CommentViewModel>> GetComments(string feedid);
        Task PostComment(string feedid, string comment);
        Task Clap(string feedid, bool clap = true);
        Task<IList<WorkoutDetailViewModel>> GetAvailableWorkouts(string type = null);
        Task<IList<WorkoutDetailViewModel>> LoadAlternatives(string name);
        string FormatTime(int seconds);
        Task SendFinishWeek(int number);
        Task Switch(int session);
        Task<Completion> GetBestData(string slug);
        Task Follow(string userid);
        void LogException(Exception ex, object detail = null);
    }

    public class Freeletics : IFreeletics
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string AuthToken { get; set; }
        public string Language { get; set; }
        public string UserId { get; set; }
        public Coach Coach { get; set; }
        public string AppVersion { get; set; }

        private TelemetryClient telemetry;
        private MyWeekViewModel WeekCache;
        private Dictionary<string, WorkoutDetailViewModel> WorkoutCache;
        private IList<WorkoutDetailViewModel> FreeWorkoutsCache;

        public Freeletics()
        {
            telemetry = new TelemetryClient();
            Language = Windows.System.UserProfile.GlobalizationPreferences.Languages.First().Substring(0, 2);
            LoadSettings();
            WorkoutCache = new Dictionary<string, WorkoutDetailViewModel>();
            FreeWorkoutsCache = new List<WorkoutDetailViewModel>();

            var pv = Package.Current.Id.Version;
            Version version = new Version(Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Revision,
                Package.Current.Id.Version.Build);

            AppVersion = "v" + version.ToString();
        }

        public bool HasLoginInfo()
        {
#if DEBUG
            return true;
#else
            return !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
#endif
        }

        public async Task<bool> Login(string username, string password)
        {
#if DEBUG
            // For debugging purposes, create a "debug-credentials.json" and "debug-credentials.local.json" file into the project directory and set your info like this:
            // { "UserName" : "your.email@domain.com", "Password" : "*********" }
            try
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///debug-credentials.json"));
                using (var sRead = new StreamReader(await file.OpenStreamForReadAsync()))
                {
                    var fileContent = await sRead.ReadToEndAsync();
                    dynamic credentials = JValue.Parse(fileContent);
                    UserName = credentials.UserName;
                    Password = credentials.Password;
                }
            }
            catch
            {
                return false;
            }
#endif

            if (username != null)
            {
                UserName = username;
                Password = password;
            }

            bool success = false;

            var logininfo = new { user = new { email = UserName, password = Password } };
            var logindata = JsonConvert.SerializeObject(logininfo);

            var url = "https://api.freeletics.com/v2/login.json";
            var client = WebRequest.CreateHttp(url);
            client.Method = "POST";
            client.ContentType = "application/json;charset=UTF-8";
            try
            {
                using (var writer = new StreamWriter(await client.GetRequestStreamAsync()))
                {
                    writer.WriteLine(logindata);
                }

                using (var response = await client.GetResponseAsync())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        dynamic data = JObject.Parse(content);

                        string token = data.user.auth_token;
                        this.AuthToken = "Token " + token;

                        try
                        {
                            Coach = new Coach();
                            Coach.Focus = data.user.fitness_profile.coach_focus;
                        }
                        catch
                        {
                            Coach = null;
                        }
                        
                        UserId = data.user.id;
                        telemetry.Context.User.Id = UserId;
                        telemetry.Context.User.AccountId = UserId;

                        telemetry.TrackEvent("Login", new Dictionary<string, string> { { "userid", UserId } });

                        success = true;

                        SaveSettings();
                    }
                }
            }
            catch { }

            return success;
        }

        public void Disconnect()
        {
            UserName = Password = AuthToken = null;
        }

        protected HttpWebRequest GetRequest(string url, string method = "GET")
        {
            var client = WebRequest.CreateHttp(url);

            client.Method = method;
            client.Headers["Authorization"] = AuthToken;
            client.Headers["Accept-Language"] = Language;
            client.Headers["User-Agent"] = "Maso for Windows Phone; " + AppVersion;

            return client;
        }

        public async Task<MyWeekViewModel> GetMyWeek()
        {
            try
            {
                var myweek = new MyWeekViewModel();

                var url = "https://api.freeletics.com/v2/coach/weeks/current.json";
                var client = GetRequest(url);

                using (var response = await client.GetResponseAsync())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        dynamic data = JObject.Parse(content);

                        myweek.Number = data.week.number;
                        int idx = 0, id = 0;
                        var active = 1;

                        var switchable = new List<int>();
                        foreach (var sw in data.week.switchable_sessions)
                        {
                            int session = sw;
                            switchable.Add(session);
                        }

                        foreach (var session in data.week.sessions)
                        {
                            idx++;
                            var training = new TrainingViewModel() { Id = idx };
                            if (switchable.Contains(idx - 1)) training.Switchable = true;

                            foreach (var sub in session)
                            {
                                dynamic t = sub.training;
                                dynamic w = sub.workout;

                                string time = null, when = null;
                                bool pb = false, star = false;
                                bool done = (t != null);
                                if (done)
                                {
                                    pb = t.personal_best == "True";
                                    star = t.star == "True";
                                    int sec = t.seconds;
                                    time = FormatTime(sec);
                                    string performedAt = t.performed_at;
                                    var performedDate = DateTime.SpecifyKind(DateTime.Parse(performedAt), DateTimeKind.Utc).ToLocalTime();
                                    when = NiceDate(performedDate);
                                }

                                var wvm = new WorkoutViewModel()
                                {
                                    Id = id++,
                                    IdxWeek = idx,
                                    Name = w.base_name,
                                    Title = GetWorkoutDisplayName(w),
                                    Slug = w.slug,
                                    WorkoutType = w.fitness_variant,
                                    Time = time,
                                    PB = pb,
                                    Star = star,
                                    When = when
                                };

                                if (w.category_slug == "home") wvm.WorkoutType = "2x2 " + wvm.WorkoutType;

                                training.Workouts.Add(wvm);
                            }

                            if (training.Workouts.All(w => !string.IsNullOrEmpty(w.Time)))
                            {
                                active = idx + 1;
                            }
                            else if (active == idx)
                            {
                                foreach (var w in training.Workouts) w.Active = true;
                            }

                            myweek.Trainings.Add(training);
                        }
                    }
                }

                Coach.SessionsNumber = myweek.Trainings.Count;

                WeekCache = myweek;
                return myweek;
            }
            catch
            {
                WeekCache = null;
                return null;
            }
        }

        private string NiceDate(DateTime performedDate)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - performedDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return "a few seconds ago";
            }
            if (delta < 5 * MINUTE)
            {
                return "a few minutes ago";
            }
            if (delta < 45 * MINUTE)
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 90 * MINUTE)
            {
                return "an hour ago";
            }
            if (delta < 24 * HOUR)
            {
                return ts.Hours + " hours ago";
            }
            if (delta < 48 * HOUR)
            {
                return "yesterday";
            }
            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }
            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        internal string GetWorkoutDisplayName (dynamic workout)
        {
            if (workout == null) return "";

            string name = workout.base_name ?? "";
            string title = name;
            string volume = workout.volume_description;
            if (volume != null && volume.StartsWith("x"))
            {
                var rep = int.Parse(volume.Substring(1));
                if (rep > 1)
                {
                    title = rep + "X " + name.ToUpper();
                }
                else
                {
                    title = name.ToUpper();
                }
            }
            else
            {
                title = volume + " " + name.ToUpper();
            }

            return title;
        }

        public async Task<MyWeekViewModel> GetMyWeekFromCache()
        {
            if (WeekCache == null)
            {
                await GetMyWeek();
            }
            return WeekCache;
        }

        public async Task<WorkoutDetailViewModel> GetWorkoutDetail(string slug)
        {
            var wvm = new WorkoutDetailViewModel();

            var url = "https://api.freeletics.com/v2/coach/workouts/{0}.json";
            var client = GetRequest(string.Format(url, slug));

            using (var response = await client.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    dynamic w = data.workout;

                    wvm.Name = w.base_name;
                    wvm.Title = GetWorkoutDisplayName(w);
                    wvm.Variant = w.fitness_variant;
                    if (w.category_slug == "home") wvm.Variant = "2x2 " + wvm.Variant;

                    var exos = new List<ExercisesViewModel>();
                    foreach (dynamic ex in data.exercises)
                    {
                        string video = "";
                        try { video = ex.video_urls.mp4; } catch { }

                        exos.Add(new ExercisesViewModel()
                        {
                            Slug = ex.slug,
                            Title = ex.title,
                            Image = ex.picture_urls.small_mobile,
                            Video = video
                        });
                    }

                    var idx = 0;
                    foreach (var round in data.workout.rounds)
                    {
                        wvm.Exercises.Add(new ExerciesSeparatorViewModel() { Title = "Round " + ++idx });
                        foreach (var ex in round)
                        {
                            var id = (string)ex.exercise_slug;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                var e = exos.FirstOrDefault(x => x.Slug == id).Clone();
                                e.Quantity = ex.quantity;
                                e.Repetition = ex.quantity_description;
                                e.Title = e.Repetition + " " + e.Title;

                                wvm.Exercises.Add(e);
                            }
                        }
                    }
                }
            }

            WorkoutCache[slug] = wvm;
            return wvm;
        }

        public WorkoutDetailViewModel GetWorkoutFromCache(string slug)
        {
            return WorkoutCache[slug];
        }

        public async Task<IList<LeaderViewModel>> GetLeaderboard(string slug)
        {
            var leaders = new List<LeaderViewModel>();

            var url = "https://api.freeletics.com/v2/coach/workouts/{0}/leaderboards/followings.json";
            var client = GetRequest(string.Format(url, slug));

            using (var response = await client.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    if (data.leaderboard_entries != null)
                    {
                        foreach (var entry in data.leaderboard_entries)
                        {
                            string who = entry.user.first_name + " " + entry.user.last_name;
                            int level = entry.user.level;
                            string avatar = entry.user.profile_pictures.small;
                            bool pb = entry.training.personal_best == "True";
                            int seconds = entry.training.seconds;
                            bool star = entry.training.star == "True";

                            string performedAt = entry.training.performed_at;
                            var performedDate = DateTime.SpecifyKind(DateTime.Parse(performedAt), DateTimeKind.Utc).ToLocalTime();
                            var when = NiceDate(performedDate);

                            leaders.Add(new LeaderViewModel()
                            {
                                Title = who,
                                Level = level,
                                Avatar = avatar,
                                Time = FormatTime(seconds),
                                PB = pb,
                                Star = star,
                                When = when
                            });
                        }
                    }
                }
            }

            return leaders;
        }

        public async Task<string> SendCompletion(Completion completion)
        {
            try
            {
                string slug = completion.Slug;
                string id = "";
                var url = "https://api.freeletics.com/v2/coach/workouts/{0}/trainings.json";
                var client = GetRequest(string.Format(url, slug), "POST");
                client.ContentType = "application/json;charset=UTF-8";

                using (var writer = new StreamWriter(await client.GetRequestStreamAsync()))
                {
                    var json = JsonConvert.SerializeObject(completion);
                    writer.WriteLine(json);
                }

                using (var response = await client.GetResponseAsync())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        dynamic data = JObject.Parse(content);
                        id = data.training.id;
                    }
                }

                // App Insight
                var props = new Dictionary<string, string> { { "slug", slug }, { "star", completion.Star.ToString() } };
                telemetry.TrackEvent("Workout", props);

                return id;

            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ConnectFailure || ex.Status == WebExceptionStatus.SendFailure ||
                    ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    // TODO : save data to retry later
                    var storage = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                    List<Completion> sendcache = new List<Completion>();
                    if (storage.ContainsKey("sendcache"))
                    {
                        sendcache = JsonConvert.DeserializeObject<List<Completion>>((string)storage["sendcache"]);
                    }
                    sendcache.Add(completion);

                    storage["sendcache"] = JsonConvert.SerializeObject(sendcache);

                    throw new SendLaterException();
                }
                else
                {
                    throw ex;
                }
            }
        }

        public async Task AttachPhoto(string trainingid, StorageFile file)
        {
            var url = @"https://api.freeletics.com/v2/coach/trainings/{0}.json";
            var client = GetRequest(string.Format(url, trainingid), "PATCH");
            client.ContentType = "Content-Type: multipart/form-data; boundary=----WebKitFormBoundary4Maso";

            var boundary = Encoding.UTF8.GetBytes("\r\n------WebKitFormBoundary4Maso\r\n");
            var contenttemplate = @"Content-Disposition: form-data; name=""training[picture]""; filename=""picture.{0}""\r\nContent-Type: image/{1}\r\n\r\n";
            var content = string.Format(contenttemplate, "", "");

            using (var writer = await client.GetRequestStreamAsync())
            {
                writer.Write(boundary, 0, boundary.Length);
                var header = Encoding.UTF8.GetBytes(content);
                writer.Write(header, 0, header.Length);
                using (var stream = await file.OpenReadAsync())
                {
                    var buffer = await FileIO.ReadBufferAsync(file);
                    
                }
            }

            using (var response = await client.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    //var content = reader.ReadToEnd();
                }
            }
        }

        public async Task<IEnumerable<FeedViewModel>> GetPeopleNews()
        {
            var list = new List<FeedViewModel>();

            var url = @"https://api.freeletics.com/v2/users/{0}/feed_entries/with_followings.json";
            var request = GetRequest(string.Format(url, UserId));

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    foreach (var entry in data.feed_entries)
                    {
                        string id = entry.id;
                        string user = entry.user.first_name + " " + entry.user.last_name;
                        string avatar = entry.user.profile_pictures.small;
                        dynamic obj = entry["object"];
                        int seconds = obj.seconds;
                        string time = FormatTime(seconds);
                        string workout = GetWorkoutDisplayName(obj.workout);
                        string variant = obj.workout.fitness_variant;
                        if (obj.workout.category_slug == "home") variant = variant + " 2x2";
                        string image = obj.picture.feed;
                        bool pb = obj.personal_best;
                        bool star = obj.star;
                        string desc = obj.description;
                        bool clapped = entry.is_liking == "True";
                        int clapcount = entry.likes_count;
                        int commentCount = entry.comments_count;

                        string when = "";
                        string performedAt = entry.created_at;
                        try
                        {
                            var performedDate = DateTime.SpecifyKind(DateTime.Parse(performedAt), DateTimeKind.Utc).ToLocalTime();
                            when = NiceDate(performedDate);
                        }
                        catch (Exception ex)
                        {
                            telemetry.TrackException(ex, new Dictionary<string, string> { { "date", performedAt }, { "lang", Language }, { "url", url } });
                        }

                        var feedentry = new FeedViewModel()
                        {
                            Id = id,
                            User = user,
                            Avatar = avatar,
                            Workout = workout.ToUpper(),
                            Variant = variant,
                            Image = image ?? "",
                            Time = time,
                            When = when,
                            PB = pb,
                            Star = star,
                            Description = desc,
                            HasClapped = clapped,
                            ClapCount = clapcount,
                            CommentCount = commentCount
                        };

                        list.Add(feedentry);
                    }
                }
            }

            return list;
        }

        public async Task<IEnumerable<CommentViewModel>> GetComments(string feedid)
        {
            var list = new List<CommentViewModel>();

            var url = @"https://api.freeletics.com/v2/feed_entries/{0}/comments.json?direction=desc&page=1";
            var request = GetRequest(string.Format(url, feedid));

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    foreach (var entry in data.comments)
                    {
                        string id = entry.id;
                        string user = entry.user.first_name + " " + entry.user.last_name;
                        string avatar = entry.user.profile_pictures.small;
                        string desc = entry.content;
                        string createdAt = entry.created_at;
                        string when = "";
                        try
                        {
                            var performedDate = DateTime.SpecifyKind(DateTime.Parse(createdAt), DateTimeKind.Utc).ToLocalTime();
                            when = NiceDate(performedDate);
                        }
                        catch (Exception ex)
                        {
                            telemetry.TrackException(ex, new Dictionary<string, string> { { "date", createdAt }, { "lang", Language }, { "url", url } });
                        }
                        
                        list.Insert(0, new CommentViewModel()
                        {
                             Id = id,
                             User = user,
                             Avatar = avatar,
                             Description = desc,
                             When = when
                        });
                    }
                }
            }

            return list;
        }

        public async Task PostComment(string feedid, string comment)
        {
            var url = @"https://api.freeletics.com/v2/feed_entries/{0}/comments.json";
            var request = GetRequest(string.Format(url, feedid), "POST");
            request.ContentType = "application/json;charset=UTF-8";

            var data = new { comment = new { content = comment } };
            using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                var json = JsonConvert.SerializeObject(data);
                writer.WriteLine(json);
            }

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                }
            }

            telemetry.TrackEvent("Comment");
        }

        public async Task Clap(string feedid, bool clap = true)
        {
            var url = @"https://api.freeletics.com/v2/feed_entries/{0}/like.json";
            url = string.Format(url, feedid);

            var method = clap ? "POST" : "DELETE";
            var request = GetRequest(url, method);
            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                }
            }

            // App Insight
            telemetry.TrackEvent("Clap");
        }

        protected void LoadSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.ContainsKey("UserName") && settings.Values.ContainsKey("Password"))
            {
                UserName = settings.Values["UserName"] as string;
                Password = settings.Values["Password"] as string;
            }
        }

        protected void SaveSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            settings.Values["UserName"] = UserName;
            settings.Values["Password"] = Password;
        }

        public string FormatTime(int seconds)
        {
            if (seconds == 0) return "";

            var strtime = "";
            var time = TimeSpan.FromSeconds(seconds);
            if (time.TotalHours >= 1)
            {
                strtime = time.ToString(@"h\:m\:ss");
            }
            else if (time.TotalMinutes >= 1)
            {
                strtime = time.ToString(@"m\:ss");
            }
            else
            {
                strtime = time.ToString(@"ss");
            }

            return strtime;
        }
        
        internal static string GetTimeIcon(bool pB, bool star, bool black = false)
        {
            string image = "ms-appx:///Assets/";
            if (pB && star)
            {
                image += "time-pb-star";
            }
            else if (pB)
            {
                image += "time-pb";
            }
            else if (star)
            {
                image += "time-star";
            }
            else
            {
                image += "time";
            }

            if (black) image += "-black";

            return image + ".png";
        }

        public async Task<IList<WorkoutDetailViewModel>> GetAvailableWorkouts(string type = null)
        {
            if (type == null)
            {
                return FreeWorkoutsCache;
            }

            FreeWorkoutsCache.Clear();
            
            string url;
            if (type == "WORKOUTS")
            {
                url = @"https://api.freeletics.com/v2/coach/workouts.json?base_volume=true&category_slugs=regular&fitness_variants=standard";
            } 
            else if (type == "EXERCICES")
            {
                url = @"https://api.freeletics.com/v2/coach/workouts.json?base_volume=true&category_slugs%5B%5D=exercise_with_distance&category_slugs%5B%5D=exercise_with_repetitions";
            }
            else
            {
                url = @"https://api.freeletics.com/v2/coach/workouts.json?category_slugs=distance_run";
            }
            
            var request = GetRequest(url);

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    var exos = new List<ExercisesViewModel>();
                    foreach (var ex in data.exercises)
                    {
                        string video = "";
                        try { video = ex.video_urls.mp4; } catch { }

                        var exercice = new ExercisesViewModel()
                        {
                            Slug = ex.slug,
                            Title = ex.title,
                            Image = ex.picture_urls.small_mobile,
                            Video = video
                        };
                        exos.Add(exercice);
                    }
                    
                    foreach (var w in data.workouts)
                    {
                        string title;
                        if (type == "WORKOUTS")
                        {
                            title = GetWorkoutDisplayName(w);
                        }
                        else
                        {
                            title = w.title;
                            title = title.ToUpper();
                        }

                        var detail = new WorkoutDetailViewModel()
                        {
                            Name = w.base_name,
                            Title = title,
                            Slug = w.slug,
                            Variant = w.fitness_variant,
                            TrainingType = type,
                            Points = w.points
                        };

                        if (w.category_slug == "home") detail.Variant = "2x2 " + detail.Variant;

                        var idx = 0;
                        foreach (var round in w.rounds)
                        {
                            detail.Exercises.Add(new ExerciesSeparatorViewModel() { Title = "Round " + ++idx });
                            foreach (var ex in round)
                            {
                                var exid = (string)ex.exercise_slug;
                                if (!string.IsNullOrWhiteSpace(exid))
                                {
                                    var e = exos.FirstOrDefault(x => x.Slug == exid).Clone();
                                    e.Quantity = ex.quantity;
                                    e.Repetition = ex.quantity_description;
                                    e.Title = e.Repetition + " " + e.Title;

                                    detail.Exercises.Add(e);
                                }
                            }
                        }

                        FreeWorkoutsCache.Add(detail);
                    }
                }
            }

            if (type == "RUN") FreeWorkoutsCache = FreeWorkoutsCache.OrderBy(w => w.Points).ToList();
            return FreeWorkoutsCache;
        }

        public async Task<IList<WorkoutDetailViewModel>> LoadAlternatives(string name)
        {
            var list = new List<WorkoutDetailViewModel>();

            string url = @"https://api.freeletics.com/v2/coach/workouts.json?base_name=" + name;
            var request = GetRequest(url);

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);

                    var exos = new List<ExercisesViewModel>();
                    foreach (var ex in data.exercises)
                    {
                        string video = "";
                        try { video = ex.video_urls.mp4; } catch { }

                        var exercice = new ExercisesViewModel()
                        {
                            Slug = ex.slug,
                            Title = ex.title,
                            Image = ex.picture_urls.small_mobile,
                            Video = video
                        };
                        exos.Add(exercice);
                    }

                    foreach (var w in data.workouts)
                    {
                        var detail = new WorkoutDetailViewModel()
                        {
                            Name = w.base_name,
                            Title = GetWorkoutDisplayName(w),
                            Slug = w.slug,
                            Variant = w.fitness_variant,
                        };

                        if (w.category_slug == "home") detail.Variant = "2x2 " + detail.Variant;

                        var idx = 0;
                        foreach (var round in w.rounds)
                        {
                            detail.Exercises.Add(new ExerciesSeparatorViewModel() { Title = "Round " + ++idx });
                            foreach (var ex in round)
                            {
                                var exid = (string)ex.exercise_slug;
                                if (!string.IsNullOrWhiteSpace(exid))
                                {
                                    var e = exos.FirstOrDefault(x => x.Slug == exid).Clone();
                                    e.Quantity = ex.quantity;
                                    e.Repetition = ex.quantity_description;
                                    e.Title = e.Repetition + " " + e.Title;

                                    detail.Exercises.Add(e);
                                }
                            }
                        }

                        list.Add(detail);
                    }
                }
            }

            return list;
        }

        public async Task SendFinishWeek(int number)
        {
            var url = @"https://api.freeletics.com/v2/coach/weeks/{0}/next.json";
            url = string.Format(url, number);

            var request = GetRequest(url, "PATCH");
            request.ContentType = "application/json;charset=UTF-8";

            var data = new { feedback = new {
                coach_focus = Coach.Focus,
                health_limitations = Coach.HealthLimitation,
                sessions_number = Coach.SessionsNumber }
            };

            using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                var json = JsonConvert.SerializeObject(data);
                writer.WriteLine(json);
            }

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                }
            }

            // App Insight
            telemetry.TrackEvent("EndWeek");
        }

        public async Task Switch(int session)
        {
            var url = @"https://api.freeletics.com/v2/coach/week_days/{0}/switch";
            url = string.Format(url, session);

            var request = GetRequest(url, "PATCH");

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                }
            }
        }

        public async Task<Completion> GetBestData(string slug)
        {
            Completion completion = null;

            var url = @"https://api.freeletics.com/v2/coach/users/{0}/trainings/best.json?workout_slugs%5B%5D={1}";
            url = string.Format(url, UserId, slug);

            var request = GetRequest(url);

            using (var response = await request.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    dynamic data = JObject.Parse(content);
                    
                    var training = data.trainings.First;
                    if (training != null)
                    {
                        completion = new Completion();
                        completion.Seconds = training.seconds;

                        if (training.exercises_seconds != null)
                        {
                            var list = new List<List<int>>();
                            foreach (var exercice in training.exercises_seconds)
                            {
                                var workouts = new List<int>();
                                foreach (var w in exercice)
                                {
                                    int sec = w;
                                    workouts.Add(sec);
                                }
                                list.Add(workouts);
                            }

                            completion.ExercisesSeconds = list.Select(l => l.ToArray()).ToArray();
                        }
                    }
                }
            }

            return completion;
        }

        public async Task Follow(string userid)
        {
            var url = "https://api.freeletics.com/v2/users/{0}/follow.json";
            var client = GetRequest(string.Format(url, userid), "POST");
            
            using (var response = await client.GetResponseAsync())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                }
            }
        }

        public void LogException(Exception ex, object detail = null)
        {
            try
            {
                if (detail == null)
                {
                    telemetry.TrackException(ex);
                }
                else
                {
                    telemetry.TrackException(ex, new Dictionary<string, string> { { "detail", JsonConvert.SerializeObject(detail) } });
                }
            }
            catch { }
        }
    }
}
