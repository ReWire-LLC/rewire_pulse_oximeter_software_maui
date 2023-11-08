using Plugin.Maui.Audio;
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
        private DeviceConnectionState _device_connection_state = DeviceConnectionState.WaitForOperatingSystemToBeReady;

        private bool _connect_flag = false;

        private string _pulse_oximeter_firmware_version_str = "1.0";
        private double _pulse_oximeter_firmware_version = 1.0;
        private int _firmware_version_request_count = 0;
        private int _firmware_version_max_request_count = 3;
        private DateTime _last_firmware_version_request_time = DateTime.MinValue;
        private TimeSpan _firmware_version_request_interval = TimeSpan.FromSeconds(250);

        private bool _recording = false;
        private string _recording_file = string.Empty;
        private DateTime _recording_start_time = DateTime.MinValue;
        private Stopwatch _recording_stopwatch = new Stopwatch();
        private RecordingState _recording_state = RecordingState.NotRecording;
        private StreamWriter? _recording_writer = null;
        private DateTime _recording_last_ui_update_time = DateTime.MinValue;

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
                System.Diagnostics.Debug.WriteLine(e.Error.ToString());
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
                switch (ConnectionState)
                {
                    case DeviceConnectionState.WaitForOperatingSystemToBeReady:
                        bool platform_initialization_result = _pulse_oximeter_device.PlatformInitialization();
                        if (platform_initialization_result)
                        {
                            ConnectionState = DeviceConnectionState.NoDevice;
                        }
                        else
                        {
                            //Sleep for a bit and then we will try again
                            Thread.Sleep(1000);
                        }

                        break;
                    case DeviceConnectionState.NoDevice:

                        ConnectionState = DeviceConnectionState.SearchingForDevice;
                        break;
                    case DeviceConnectionState.SearchingForDevice:

                        bool device_found = _pulse_oximeter_device.ScanForDevice();
                        if (device_found)
                        {
                            ConnectionState = DeviceConnectionState.RequestingPermission;
                        }

                        break;

                    case DeviceConnectionState.RequestingPermission:

                        bool has_permissions = _pulse_oximeter_device.CheckPermissions();
                        if (!has_permissions)
                        {
                            _pulse_oximeter_device.RequestPermissions();
                        }
                        else
                        {
                            ConnectionState = DeviceConnectionState.ConnectingToDevice;
                        }

                        break;

                    case DeviceConnectionState.ConnectingToDevice:

                        _pulse_oximeter_device.Connect();
                        if (_pulse_oximeter_device.IsConnected)
                        {
                            _pulse_oximeter_device.SendStreamOnCommand();
                            ConnectionState = DeviceConnectionState.Connected_RequestFirmwareVersion;
                        }

                        break;

                    case DeviceConnectionState.Connected_RequestFirmwareVersion:

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            BackgroundThread_HandleFirmwareVersionRequest();
                        }
                        else
                        {
                            ConnectionState = DeviceConnectionState.NoDevice;
                        }
                        
                        break;
                    case DeviceConnectionState.Connected:

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            BackgroundThread_HandleConnectedDevice();
                        }
                        else
                        {
                            ConnectionState = DeviceConnectionState.NoDevice;
                        }

                        break;
                }
            }
        }

        private void BackgroundThread_NotifyPropertyChanged(string property_name)
        {
            _background_thread.ReportProgress(1, property_name);
        }

        private void BackgroundThread_HandleFirmwareVersionRequest()
        {
            //Grab each line of new data
            List<string> input_data = _pulse_oximeter_device.GetData();

            //Process each line of new data
            foreach (var current_line in input_data)
            {
                if (!string.IsNullOrEmpty(current_line) && current_line.StartsWith("[VERSION]"))
                {
                    //Sometimes we get malformed lines (probably due to serial buffer issues on Android's side)
                    //When this happens, sometimes we will have a short line of truncasted data followed by
                    //another set of data on the same line. So the string "[VERSION]" appears twice. If we detect
                    //this, then let's just skip this line.
                    if (current_line.LastIndexOf("[VERSION]") > 0)
                    {
                        continue;
                    }

                    //Split the line into its component parts
                    var split_current_line = current_line.Split(' ').ToList();
                    if (split_current_line.Count >= 2)
                    {
                        var version_string = split_current_line[1];
                        _pulse_oximeter_firmware_version_str = version_string.Trim();
                        bool parse_success = double.TryParse(_pulse_oximeter_firmware_version_str, out double result);
                        if (parse_success)
                        {
                            _pulse_oximeter_firmware_version = result;

                            //If we reach this point in the code, we are done. We have the firmware version. Let's
                            //proceed to the next state

                            //Enable pulse ox device streaming
                            _pulse_oximeter_device.SendStreamOnCommand();

                            //Proceed to the next state
                            ConnectionState = DeviceConnectionState.Connected;

                            //Return immediately
                            return;
                        }
                    }
                }
            }

            //Check to see if we should still request the firmware version
            if (_firmware_version_request_count < _firmware_version_max_request_count)
            {
                //Check to see if enough time has passed since our last attempt to request the firmware version
                if (DateTime.Now >= (_last_firmware_version_request_time + _firmware_version_request_interval))
                {
                    //If so, request the firmware version
                    _pulse_oximeter_device.SendVersionCommand();
                    _last_firmware_version_request_time = DateTime.Now;
                    _firmware_version_request_count++;
                }
            }
            else
            {
                //If we have exceeded the maximum number of allowed attempts, then we will automatically
                //assume that the firmware is version 1.0.

                //Enable pulse ox device streaming
                _pulse_oximeter_device.SendStreamOnCommand();

                //Proceed to the next state
                ConnectionState = DeviceConnectionState.Connected;
            }
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
                if (!string.IsNullOrEmpty(current_line) && current_line.StartsWith("[DATA]"))
                {
                    //Sometimes we get malformed lines (probably due to serial buffer issues on Android's side)
                    //When this happens, sometimes we will have a short line of truncasted data followed by
                    //another set of data on the same line. So the string "[DATA]" appears twice. If we detect
                    //this, then let's just skip this line.
                    if (current_line.LastIndexOf("[DATA]") > 0)
                    {
                        continue;
                    }

                    //Split the line into its component parts
                    var split_current_line = current_line.Split('\t').ToList();
                    if (_pulse_oximeter_firmware_version == 1.0 && split_current_line.Count == 9)
                    {
                        //A well-formed line of data should have exactly nine components
                        //The first component is simply the "[DATA]" string
                        //The next 8 components are all integers

                        List<int> current_line_values = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        bool parse_success = true;
                        for (int i = 1; i < 9; i++)
                        {
                            parse_success = Int32.TryParse(split_current_line[i], out int result);
                            if (!parse_success)
                            {
                                break;
                            }
                            else
                            {
                                current_line_values[i] = result;
                            }
                        }
                        
                        if (parse_success)
                        {
                            //Get the IR, HR, and SpO2 components
                            var ir = current_line_values[1];
                            var hr = current_line_values[3];
                            var spo2 = current_line_values[5];

                            //Set the values on the model
                            IR = ir;

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
                    else if (_pulse_oximeter_firmware_version > 1.0 && split_current_line.Count >= 11)
                    {
                        //Parse the first 10 elements of the line as integers
                        List<int> current_line_values = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        bool parse_success = true;
                        for (int i = 1; i < 10; i++)
                        {
                            parse_success = Int32.TryParse(split_current_line[i], out int result);
                            if (!parse_success)
                            {
                                break;
                            }
                            else
                            {
                                current_line_values[i] = result;
                            }
                        }

                        //The last element of the line is the perfusion index, parse it as a double
                        var perfusion_index_str = split_current_line[10];
                        var pi_parse_success = Double.TryParse(perfusion_index_str, out double pi);

                        //If parsing was successful...
                        if (parse_success && pi_parse_success)
                        {
                            //Get the IR, HR, and SpO2 components
                            var ir = current_line_values[2];
                            var hr = current_line_values[4];
                            var spo2 = current_line_values[6];

                            //Set the values on the model
                            IR = ir;

                            if (_stopwatch.ElapsedMilliseconds >= 1000)
                            {
                                HeartRate = hr;
                                SpO2 = spo2;
                                PerfusionIndex = pi;

                                _stopwatch.Restart();
                            }

                            //Handle recording of data
                            BackgroundThread_HandleRecording(current_line.Substring(7).Replace("\t", ", "));
                        }
                    }
                }
            }
        }

        private void BackgroundThread_HandleRecording(string data_row)
        {
            switch (_recording_state)
            {
                case RecordingState.NotRecording:

                    //If the user has requested to start a recording, move to the "launching recording" state
                    if (_recording && !string.IsNullOrWhiteSpace(_recording_file))
                    {
                        _recording_state = RecordingState.LaunchingRecording;
                    }

                    break;
                case RecordingState.LaunchingRecording:

                    //In this state, we attempt to open a file for writing
                    try
                    {
                        var output_stream = FolderManager_CrossPlatform.GetInstance().OpenFileForWriting(_recording_file);
                        _recording_writer = new StreamWriter(output_stream, Encoding.UTF8);
                        _recording_state = RecordingState.Recording;

                        string first_line = "Milliseconds, IR, Red, HR, HR Confidence, SpO2, Algorithm State, Algorithm Status, Interbeat Interval";
                        _recording_writer.WriteLine(first_line);

                        _recording_start_time = DateTime.Now;
                        _recording_stopwatch.Restart();
                    }
                    catch (Exception ex)
                    {
                        _recording_writer = null;
                        _recording_state = RecordingState.NotRecording;
                        _recording = false;
                        _recording_stopwatch.Stop();
                        BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                    }

                    break;
                case RecordingState.Recording:

                    double ts = _recording_stopwatch.ElapsedMilliseconds;
                    string new_line = ts + ", " + data_row;
                    try
                    {
                        //Record the current line of data
                        _recording_writer?.WriteLine(new_line);

                        //Check to see if the UI needs to be updated with recording information
                        if (DateTime.Now >= (_recording_last_ui_update_time + TimeSpan.FromSeconds(1)))
                        {
                            BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                        }

                        //Check to see the recording has been stopped/cancelled
                        if (!_recording)
                        {
                            //If so, go to the "stopping recording" state
                            _recording_state = RecordingState.StoppingRecording;
                        }
                    }
                    catch (Exception ex)
                    {
                        _recording_writer = null;
                        _recording_state = RecordingState.NotRecording;
                        _recording = false;
                        _recording_stopwatch.Stop();
                        BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));
                    }

                    break;
                case RecordingState.StoppingRecording:

                    try
                    {
                        _recording_writer?.Close();
                        _recording_stopwatch.Stop();
                    }
                    catch (Exception ex)
                    {
                        //empty
                    }

                    _recording_state = RecordingState.NotRecording;
                    BackgroundThread_NotifyPropertyChanged(nameof(IsRecording));

                    break;
            }
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
                        _current_audio_player = AudioManager.Current.CreatePlayer(BackgroundThread_GenerateAlarmSound(generate_double));
                        _current_audio_player.Play();

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

            SineGenerator signal_data = new SineGenerator(440, 44100, 400, 25);
            SilenceGenerator silence_data = new SilenceGenerator(44100, 500);

            data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
            data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
            data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
            data_chunk.AddSampleData(silence_data.Data, silence_data.Data);

            header.FileLength += format.Length() + data_chunk.Length();

            temp_bytes.AddRange(header.GetBytes());
            temp_bytes.AddRange(format.GetBytes());
            temp_bytes.AddRange(data_chunk.GetBytes());

            var byte_array = temp_bytes.ToArray();
            MemoryStream memory_stream = new MemoryStream(byte_array);
            return memory_stream;
        }

        private MemoryStream BackgroundThread_GenerateAlarmSound (bool generate_double)
        {
            List<byte> temp_bytes = new List<byte>();

            WaveHeader header = new WaveHeader();
            FormatChunk format = new FormatChunk();
            DataChunk data_chunk = new DataChunk();

            SineGenerator signal_data = new SineGenerator(440, 44100, 175, 25);
            SilenceGenerator silence_data = new SilenceGenerator(44100, 80);
            SilenceGenerator silence_data_2 = new SilenceGenerator(44100, 350);

            int num_times = 1;
            if (generate_double)
            {
                num_times = 2;
            }

            for (int i = 0; i < num_times; i++)
            {
                data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
                data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
                data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
                data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
                data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
                data_chunk.AddSampleData(silence_data_2.Data, silence_data_2.Data);
                data_chunk.AddSampleData(signal_data.Data, signal_data.Data);
                data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
                data_chunk.AddSampleData(signal_data.Data, signal_data.Data);

                if (num_times == 2)
                {
                    data_chunk.AddSampleData(silence_data_2.Data, silence_data_2.Data);
                }
                else
                {
                    data_chunk.AddSampleData(silence_data.Data, silence_data.Data);
                }
            }

            header.FileLength += format.Length() + data_chunk.Length();

            temp_bytes.AddRange(header.GetBytes());
            temp_bytes.AddRange(format.GetBytes());
            temp_bytes.AddRange(data_chunk.GetBytes());

            var byte_array = temp_bytes.ToArray();
            MemoryStream memory_stream = new MemoryStream(byte_array);
            return memory_stream;
        }

        private MemoryStream BackgroundThread_GeneratePulseSound (int pitch, int total_duration)
        {
            ushort tone_duration = 50;
            int silence_duration = total_duration - tone_duration;

            List<byte> temp_bytes = new List<byte> ();
            
            WaveHeader header = new WaveHeader();
            FormatChunk format = new FormatChunk();
            DataChunk data = new DataChunk();

            SineGenerator signal_data = new SineGenerator(pitch, 44100, tone_duration, 20);
            SilenceGenerator silence_data = new SilenceGenerator(44100, Convert.ToUInt16(silence_duration));
            data.AddSampleData(signal_data.Data, signal_data.Data);
            data.AddSampleData(silence_data.Data, silence_data.Data);

            header.FileLength += format.Length() + data.Length();

            temp_bytes.AddRange(header.GetBytes());
            temp_bytes.AddRange(format.GetBytes());
            temp_bytes.AddRange(data.GetBytes());

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
