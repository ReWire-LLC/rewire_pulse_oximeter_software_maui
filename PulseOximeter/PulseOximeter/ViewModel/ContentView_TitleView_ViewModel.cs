using PulseOximeter.Model;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.ViewModel
{
    public class ContentView_TitleView_ViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ApplicationModel _model = ApplicationModel.GetInstance();

        #endregion

        #region Constructor

        public ContentView_TitleView_ViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        [ReactToModelPropertyChanged(new string[] { "MuteAudio" })]
        public string MuteButtonText
        {
            get
            {
                if (_model.MuteAudio)
                {
                    return "Unmute";
                }
                else
                {
                    return "Mute";
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "IsRecording" })]
        public bool IsRecording
        {
            get
            {
                return (_model.IsRecording);
            }
        }

        [ReactToModelPropertyChanged(new string[] { "IsRecording" })]
        public string RecordButtonText
        {
            get
            {
                if (_model.IsRecording)
                {
                    return "Stop Recording";
                }
                else
                {
                    return "Record";
                }
            }
        }

        #endregion

        #region Methods

        public void ToggleMute()
        {
            _model.MuteAudio = !_model.MuteAudio;
        }

        public void StartRecording(string filename)
        {
            _model.StartRecording(filename);
        }

        public void StopRecording()
        {
            _model.StopRecording();
        }

        #endregion
    }
}
