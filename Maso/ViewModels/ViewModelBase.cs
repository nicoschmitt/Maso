using Caliburn.Micro;
using Maso.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Maso.ViewModels
{
    public abstract class ViewModelBase : Screen
    {
        private readonly INavigationService navigationService;

        protected ViewModelBase(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public void GoBack()
        {
            navigationService.GoBack();
        }

        public bool CanGoBack
        {
            get
            {
                return navigationService.CanGoBack;
            }
        }

        internal async void ShowError(Exception ex)
        {
            try
            {
                string message = ex.Message;
                if (ex is WebException && ((WebException)ex).Status == WebExceptionStatus.UnknownError)
                {
                    var response = ((WebException)ex).Response;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        dynamic data = JObject.Parse(content);

                        message = data.error["base"].First;
                    }
                }
                else if (ex is SendLaterException)
                {
                    message = "Network error, we will try it later. Keep working.";
                }

                var dialog = new MessageDialog(message);
                dialog.Commands.Add(new UICommand("OK"));
                dialog.DefaultCommandIndex = 0;
                await dialog.ShowAsync();
            }
            catch { }
        }
    }
}
