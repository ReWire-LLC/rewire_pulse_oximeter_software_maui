using PulseOximeter.CrossPlatform;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        #endregion

        #region Constructor

        public ApplicationModel() 
        {
            //Set up the background thread
            _background_thread.DoWork += _background_thread_DoWork;
            _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted;
            _background_thread.ProgressChanged += _background_thread_ProgressChanged;
            _background_thread.WorkerReportsProgress = true;
            _background_thread.WorkerSupportsCancellation = true;
        }

        #endregion

        #region Background thread methods

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("BACKGROUND THREAD EXITED");
            System.Diagnostics.Debug.WriteLine(e.Error.ToString());
        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_background_thread.CancellationPending)
            {
                switch (_device_connection_state)
                {
                    case DeviceConnectionState.NoDevice:

                        if (_connect_flag)
                        {
                            _connect_flag = false;
                            _pulse_oximeter_device.Open();
                        }

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            System.Diagnostics.Debug.WriteLine("REWIRE PULSE OXIMETER WAS CONNECTED");
                            _pulse_oximeter_device.SendStreamOnCommand();
                            _device_connection_state = DeviceConnectionState.Connected;
                        }

                        break;
                    case DeviceConnectionState.Connected:

                        if (_pulse_oximeter_device.IsConnected)
                        {
                            List<string> input_data = _pulse_oximeter_device.GetData();
                            foreach (var input_line in input_data)
                            {
                                System.Diagnostics.Debug.WriteLine(input_line);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("REWIRE PULSE OXIMETER WAS DISCONNECTED");
                            _device_connection_state = DeviceConnectionState.NoDevice;
                        }

                        break;
                }
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

        public void Connect ()
        {
            _connect_flag = true;
        }

        #endregion
    }
}
