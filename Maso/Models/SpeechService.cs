using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.Storage;

namespace Maso.Models
{
    public interface ISpeechService
    {
        Task InitializeVoiceCommands();
        bool IsVoiceActivated();
    }

    public class SpeechService : ISpeechService
    {
        public bool VoiceActivated { get; set; }

        public async Task InitializeVoiceCommands()
        {
            try
            {
                Uri vcdUri = new Uri("ms-appx:///VoiceCommandDefinition.xml", UriKind.Absolute);
                var file = await StorageFile.GetFileFromApplicationUriAsync(vcdUri);
                await VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(file);

                VoiceActivated = true;
            }
            catch
            {
                //voice command file not found or language not supported or file is 
                //invalid format (missing stuff), or capabilities not selected, etc etc
                VoiceActivated = false;
            }
        }

        public bool IsVoiceActivated()
        {
            return VoiceActivated;
        }
    }
}
