﻿using Plugin.Maui.Audio;
using PulseOximeter.CrossPlatform;
using PulseOximeter.Model.Audio;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public class ApplicationModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private BackgroundWorker _background_thread = new BackgroundWorker();
        private PulseOximeterDevice _pulse_oximeter_device = new PulseOximeterDevice();
        private DeviceConnectionState _device_connection_state = DeviceConnectionState.NoDevice;

        private bool _connect_flag = false;

        private bool _recording = false;
        private string _recording_file = string.Empty;
        private DateTime _recording_start_time = DateTime.MinValue;
        private Stopwatch _recording_stopwatch = new Stopwatch();
        private RecordingState _recording_state = RecordingState.NotRecording;
        private StreamWriter? _recording_writer = null;
        private DateTime _recording_last_ui_update_time = DateTime.MinValue;

        private List<DateTime> _recent_ir_value_datetimes = new List<DateTime>();
        private List<int> _recent_ir_values = new List<int>();
        private DateTime _last_perfusion_index_update_time = DateTime.MinValue;
        private TimeSpan _perfusion_index_update_period = TimeSpan.FromSeconds(1);

        private int _heart_rate = 0;
        private int _spo2 = 0;
        private int _ir = 0;
        private double _perfusion_index = 0;

        private DateTime _last_alarm_time = DateTime.MinValue;
        private TimeSpan _alarm_period = TimeSpan.FromSeconds(10);
        private DateTime _last_no_pulse_time = DateTime.MinValue;
        private TimeSpan _no_pulse_period = TimeSpan.FromSeconds(15);

        private Stopwatch _stopwatch = new Stopwatch();

        private int _alarm_hr_min = 50;
        private int _alarm_hr_max = 100;
        private int _alarm_spo2_min = 70;
        private int _alarm_spo2_max = 100;

        private bool _mute_audio = false;
        private IAudioPlayer _current_audio_player = null;

        /// <summary>
        /// This pitch mapping was taken from the following published paper: 
        /// https://array.aami.org/doi/10.2345/0899-8205-56.2.46
        /// "Signaling Patient Oxygen Desaturation with Enhanced Pulse Oximetry Tones"
        /// Biomedical Information & Technology, 2022
        /// 
        /// Other useful links/papers:
        /// 1. https://journals.lww.com/anesthesia-analgesia/fulltext/2016/05000/the_sounds_of_desaturation__a_survey_of_commercial.26.aspx
        /// </summary>
        private Dictionary<int, int> _spo2_pitch_mapping = new Dictionary<int, int>()
        {
            { 100, 881 },
            { 99, 858 },
            { 98, 836 },
            { 97, 815 },
            { 96, 794 },
            { 95, 774 },
            { 94, 754 },
            { 93, 735 },
            { 92, 716 },
            { 91, 698 },
            { 90, 680 },
            { 89, 663 },
            { 88, 646 },
            { 87, 629 },
            { 86, 613 },
            { 85, 597 },
            { 84, 582 },
            { 83, 567 },
            { 82, 553 },
            { 81, 539 },
            { 80, 525 }
        };

        #endregion

        #region Singleton Constructor

        private static volatile ApplicationModel _instance = null;
        private static object _instance_lock = new object();

        private ApplicationModel()
        {
            //Load the alarm values from the configuration class
            _alarm_hr_max = ApplicationConfiguration.HeartRateAlarmMaximum;
            _alarm_hr_min = ApplicationConfiguration.HeartRateAlarmMinimum;
            _alarm_spo2_max = ApplicationConfiguration.SpO2AlarmMaximum;
            _alarm_spo2_min = ApplicationConfiguration.SpO2AlarmMinimum;

            //Set up the background thread
            _background_thread.DoWork += _background_thread_DoWork;
            _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted;
            _background_thread.ProgressChanged += _background_thread_ProgressChanged;
            _background_thread.WorkerReportsProgress = true;
            _background_thread.WorkerSupportsCancellation = true;
        }

        public static ApplicationModel GetInstance()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ApplicationModel();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Background thread methods

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _device_connection_state = DeviceConnectionState.Error;
                NotifyPropertyChanged(nameof(ConnectionState));
            }
        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                string? property_name = e.UserState as string;
                if (!string.IsNullOrWhiteSpace(property_name))
                {
                    NotifyPropertyChanged(property_name);
                }
            }
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Do some setup work
            _stopwatch.Start();

            //Loop forever
            while (!_background_thread.CancellationPending)
            {
                switch (_device_connection_state)
                {
                    case DeviceConnectionState.WaitForOperatingSystemToBeReady:
                        bool platform_initialization_result = _pulse_oximeter_device.PlatformInitialization();
                        if (platform_initialization_result)
                        {
                            _device_connection_state = DeviceConnectionState.NoDevice;
                        }
                        else
                        {
                            //Sleep for a bit and then we will try again
                            Thread.Sleep(1000);
                        }

                        break;
                    case DeviceConnectionState.NoDevice:

                        if (_connect_flag)
                        {
                            _connect_flag = false;
                            _pulse_oximeter_device.Open();
                        }

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            _pulse_oximeter_device.SendStreamOnCommand();
                            _device_connection_state = DeviceConnectionState.Connected;
                        }

                        break;
                    case DeviceConnectionState.Connected:

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            BackgroundThread_HandleConnectedDevice();
                        }
                        else
                        {
                            _device_connection_state = DeviceConnectionState.NoDevice;
                        }

                        break;
                }
            }
        }

        private void BackgroundThread_NotifyPropertyChanged(string property_name)
        {
            _background_thread.ReportProgress(1, property_name);
        }

        private void BackgroundThread_HandleConnectedDevice ()
        {
            //Receive the pulse oximetry data
            BackgroundThread_ReceivePulseOximeterData();

            //Handle audio output
            if (!_mute_audio)
            {
                BackgroundThread_HandleAudio();
            }
        }

        private void BackgroundThread_ReceivePulseOximeterData ()
        {
            //Grab each line of new data
            List<string> input_data = _pulse_oximeter_device.GetData();

            //Process each line of new data
            foreach (var current_line in input_data)
            {
                //Split the line into its component parts
                var split_current_line = current_line.Split('\t').ToList();
                if (split_current_line.Count >= 9)
                {
                    //Get the IR, HR, and SpO2 components
                    var ir_string = split_current_line[1];
                    var hr_string = split_current_line[3];
                    var spo2_string = split_current_line[5];

                    //Convert each component from a string to an integer
                    var ir_parse_success = Int32.TryParse(ir_string, out int ir);
                    var hr_parse_success = Int32.TryParse(hr_string, out int hr);
                    var spo2_parse_success = Int32.TryParse(spo2_string, out int spo2);

                    if (hr_parse_success && spo2_parse_success && ir_parse_success)
                    {
                        IR = ir;
                        BackgroundThread_UpdatePerfusionIndex(ir);

                        if (_stopwatch.ElapsedMilliseconds >= 1000)
                        {
                            HeartRate = hr;
                            SpO2 = spo2;

                            _stopwatch.Restart();
                        }

                        //Handle recording of data
                        BackgroundThread_HandleRecording(current_line.Substring(7).Replace("\t", ", "));
                    }
                }
            }
        }

        private void BackgroundThread_UpdatePerfusionIndex(int ir)
        {
            //TO DO
        }

        private void BackgroundThread_HandleRecording(string data)
        {
            //TO DO
        }

        private void BackgroundThread_HandleAudio ()
        {
            //If we are not currently playing a sound
            if (_current_audio_player == null || (_current_audio_player != null && !_current_audio_player.IsPlaying))
            {
                //Check if heart rate is currently 0
                if (HeartRate == 0)
                {
                    if (DateTime.Now >= (_last_no_pulse_time + _no_pulse_period))
                    {
                        //Play the "no pulse found" sound
                        _current_audio_player = AudioManager.Current.CreatePlayer(BackgroundThread_GenerateNoPulseSound());
                        _current_audio_player.Play();

                        _last_no_pulse_time = DateTime.Now;
                    }
                }
                else
                {
                    //If there is a heart rate...

                    //Determine whether to play the alarm
                    bool should_alarm = (HeartRate < _alarm_hr_min || HeartRate > _alarm_hr_max || SpO2 < _alarm_spo2_min || SpO2 > _alarm_spo2_max);
                    should_alarm &= (DateTime.Now >= (_last_alarm_time + _alarm_period));

                    if (should_alarm)
                    {
                        //If we have determined that we should play an alarm...

                        //Determine whether to play a single or a double
                        Random rnd = new Random();
                        int rnd_num = rnd.Next(0, 2);
                        bool generate_double = rnd_num > 0;

                        //Play the alarm audio
                        //_current_audio_player = AudioManager.Current.CreatePlayer(BackgroundThread_GenerateAlarmSound(generate_double));
                        //_current_audio_player.Play();

                        //Set the "last alarm time" variable
                        _last_alarm_time = DateTime.Now;
                    }
                    else
                    {
                        //Otherwise, let's play the normal heart rate audio

                        //Determine the pitch of the tone
                        int pitch;
                        if (_spo2_pitch_mapping.ContainsKey(SpO2))
                        {
                            pitch = _spo2_pitch_mapping[SpO2];
                        }
                        else
                        {
                            pitch = _spo2_pitch_mapping[80];
                        }

                        //Determine how fast the tone will play
                        var total_duration = 60_000 / HeartRate;
                        
                        //Generate the tone and the silence that follows it
                        var audio_memory_stream = BackgroundThread_GeneratePulseSound(pitch, total_duration);
                        _current_audio_player = AudioManager.Current.CreatePlayer(audio_memory_stream);
                        _current_audio_player.Play();
                    }
                }
            }
        }

        private MemoryStream BackgroundThread_GenerateNoPulseSound ()
        {
            List<byte> temp_bytes = new List<byte>();

            WaveHeader header = new WaveHeader();
            FormatChunk format = new FormatChunk();
            DataChunk data_chunk = new DataChunk();
            DataChunk silence_data_chunk = new DataChunk();
            DataChunk data_chunk2 = new DataChunk();
            DataChunk silence_data_chunk2 = new DataChunk();

            SineGenerator signal_data = new SineGenerator(200, 44100, 400);
            SilenceGenerator silence_data = new SilenceGenerator(44100, 500);

            data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
            silence_data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
            data_chunk2.AddSampleData(signal_data.Data, signal_data.Data);
            silence_data_chunk2.AddSampleData(silence_data.Data, silence_data.Data);

            header.FileLength += format.Length() + data_chunk.Length() + silence_data_chunk.Length() + data_chunk2.Length() + silence_data_chunk2.Length();

            temp_bytes.AddRange(header.GetBytes());
            temp_bytes.AddRange(format.GetBytes());
            temp_bytes.AddRange(data_chunk.GetBytes());
            temp_bytes.AddRange(silence_data_chunk.GetBytes());
            temp_bytes.AddRange(data_chunk2.GetBytes());
            temp_bytes.AddRange(silence_data_chunk2.GetBytes());

            var byte_array = temp_bytes.ToArray();
            MemoryStream memory_stream = new MemoryStream(byte_array);
            return memory_stream;
        }

        private MemoryStream BackgroundThread_GenerateAlarmSound (bool generate_double)
        {
            return null;
        }

        private MemoryStream BackgroundThread_GeneratePulseSound (int pitch, int total_duration)
        {
            ushort tone_duration = 50;
            int silence_duration = total_duration - tone_duration;

            List<byte> temp_bytes = new List<byte> ();
            
            WaveHeader header = new WaveHeader();
            FormatChunk format = new FormatChunk();
            DataChunk data = new DataChunk();
            DataChunk silence_data_chunk = new DataChunk();

            SineGenerator signal_data = new SineGenerator(pitch, 44100, tone_duration);
            SilenceGenerator silence_data = new SilenceGenerator(44100, Convert.ToUInt16(silence_duration));
            data.AddSampleData(signal_data.Data, signal_data.Data);
            silence_data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
            header.FileLength += format.Length() + data.Length() + silence_data_chunk.Length();

            temp_bytes.AddRange(header.GetBytes());
            temp_bytes.AddRange(format.GetBytes());
            temp_bytes.AddRange(data.GetBytes());
            temp_bytes.AddRange(silence_data_chunk.GetBytes());

            var byte_array = temp_bytes.ToArray();
            MemoryStream memory_stream = new MemoryStream(byte_array);
            return memory_stream;
        }

        #endregion

        #region Properties

        public int IR
        {
            get
            {
                return _ir;
            }
            private set
            {
                _ir = value;
                BackgroundThread_NotifyPropertyChanged(nameof(IR));
            }
        }

        public int HeartRate
        {
            get
            {
                return _heart_rate;
            }
            private set
            {
                _heart_rate = value;
                BackgroundThread_NotifyPropertyChanged(nameof(HeartRate));
            }
        }

        public int SpO2
        {
            get
            {
                return _spo2;
            }
            private set
            {
                _spo2 = value;
                BackgroundThread_NotifyPropertyChanged(nameof(SpO2));
            }
        }

        public double PerfusionIndex
        {
            get
            {
                return _perfusion_index;
            }
            private set
            {
                _perfusion_index = value;
                BackgroundThread_NotifyPropertyChanged(nameof(PerfusionIndex));
            }
        }

        public int HeartRateAlarm_Minimum
        {
            get
            {
                return _alarm_hr_min;
            }
            set
            {
                if (value != _alarm_hr_min)
                {
                    _alarm_hr_min = value;
                    ApplicationConfiguration.HeartRateAlarmMinimum = value;
                }
            }
        }

        public int HeartRateAlarm_Maximum
        {
            get
            {
                return _alarm_hr_max;
            }
            set
            {
                if (value != _alarm_hr_max)
                {
                    _alarm_hr_max = value;
                    ApplicationConfiguration.HeartRateAlarmMaximum = value;
                }
            }
        }

        public int SpO2Alarm_Minimum
        {
            get
            {
                return _alarm_spo2_min;
            }
            set
            {
                if (value != _alarm_spo2_min)
                {
                    _alarm_spo2_min = value;
                    ApplicationConfiguration.SpO2AlarmMinimum = value;
                }
            }
        }

        public int SpO2Alarm_Maximum
        {
            get
            {
                return _alarm_spo2_max;
            }
            set
            {
                if (value != _alarm_spo2_max)
                {
                    _alarm_spo2_max = value;
                    ApplicationConfiguration.SpO2AlarmMaximum = value;
                }
            }
        }

        public DeviceConnectionState ConnectionState
        {
            get
            {
                return _device_connection_state;
            }
            private set
            {
                _device_connection_state = value;
                BackgroundThread_NotifyPropertyChanged(nameof(ConnectionState));
            }
        }

        public bool MuteAudio
        {
            get
            {
                return _mute_audio;
            }
            set
            {
                _mute_audio = value;
                NotifyPropertyChanged(nameof(MuteAudio));
            }
        }

        public bool IsRecording
        {
            get
            {
                return _recording;
            }
        }

        public DateTime RecordingStartTime
        {
            get
            {
                return _recording_start_time;
            }
        }

        #endregion

        #region Public Methods

        public void Start ()
        {
            if (_background_thread != null && !_background_thread.IsBusy)
            {
                _background_thread.RunWorkerAsync();
            }
        }

        public void StartRecording(string filename)
        {
            if (!_recording)
            {
                _recording_file = filename;
                _recording = true;
                NotifyPropertyChanged(nameof(IsRecording));
            }
        }

        public void StopRecording()
        {
            if (_recording)
            {
                _recording = false;
                NotifyPropertyChanged(nameof(IsRecording));
            }
        }

        public void Connect ()
        {
            _connect_flag = true;
        }

        #endregion
    }
}
