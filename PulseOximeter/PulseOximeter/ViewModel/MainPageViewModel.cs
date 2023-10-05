using PulseOximeter.Model;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.ViewModel
{
    public class MainPageViewModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private ApplicationModel _model;

        #endregion

        #region Constructor

        public MainPageViewModel()
        {
            _model = ApplicationModel.GetInstance();
        }

        #endregion

        #region Properties

        [ReactToModelPropertyChanged(new string[] { "HeartRate" })]
        public string HeartRate
        {
            get
            {
                return _model.HeartRate.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SpO2" })]
        public string SpO2
        {
            get
            {
                return _model.SpO2.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "HeartRate" })]
        public SolidColorBrush HeartRateBackground
        {
            get
            {
                if (_model.HeartRate < _model.HeartRateAlarm_Minimum || _model.HeartRate > _model.HeartRateAlarm_Maximum)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SpO2" })]
        public SolidColorBrush SpO2Background
        {
            get
            {
                if (_model.SpO2 < _model.SpO2Alarm_Minimum || _model.SpO2 > _model.SpO2Alarm_Maximum)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "ConnectionState", "IsRecording" })]
        public string ConnectionState
        {
            get
            {
                string result = DeviceConnectionStateConverter.ConvertToDescription(_model.ConnectionState);
                if (_model.IsRecording)
                {
                    var recording_duration_str = (DateTime.Now - _model.RecordingStartTime).ToString(@"hh\:mm\:ss");
                    result += " (Recording: " + recording_duration_str + ")";
                }

                return result;
            }
        }

        [ReactToModelPropertyChanged(new string[] { "IsRecording" })]
        public bool IsRecording
        {
            get
            {
                return _model.IsRecording;
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

        public void Connect ()
        {
            _model.Connect();
        }

        #endregion
    }
}
