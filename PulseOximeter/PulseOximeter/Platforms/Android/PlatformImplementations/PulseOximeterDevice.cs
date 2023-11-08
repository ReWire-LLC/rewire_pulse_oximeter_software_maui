using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.Driver;
using PulseOximeter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.CrossPlatform
{
    public partial class PulseOximeterDevice
    {
        #region Private members

        private UsbReceiver _usb_receiver = UsbReceiver.GetInstance();
        private UsbManager _usb_manager = null;

        private UsbDevice _selected_usb_device = null;
        private UsbDeviceConnection _usb_device_connection = null;
        private UsbSerialPort _serial_port = null;

        private List<byte> _buffer = new List<byte>();

        #endregion

        #region Private Partial Methods

        private partial bool IsConnectedAndStreaming()
        {
            return (_serial_port != null);
        }

        #endregion

        #region Event Handlers

        private void Handle_PermissionGranted(object sender, global::Android.Hardware.Usb.UsbDevice e)
        {
            //OnPermissionGranted(e);
        }

        private void Handle_DeviceDetached(object sender, global::Android.Hardware.Usb.UsbDevice e)
        {
            //OnDeviceDetached(e);
        }

        private void Handle_DeviceAttached(object sender, global::Android.Hardware.Usb.UsbDevice e)
        {
            //OnDeviceAttached(e);
        }

        #endregion

        #region Public Partial Methods

        public partial bool PlatformInitialization()
        {
            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (current_activity != null)
            {
                //Get the USB manager
                _usb_manager = current_activity.ApplicationContext.GetSystemService(Android.Content.Context.UsbService) as Android.Hardware.Usb.UsbManager;
            }

            return (current_activity != null && _usb_manager != null);
        }

        public partial bool ScanForDevice()
        {
            //If the "connected usb device" object is currently null...
            if (_selected_usb_device == null && _usb_manager != null)
            {
                //Grab the Android USB manager and get a list of connected devices
                var attached_devices = _usb_manager.DeviceList;

                //Find the pulse oximeter in the list of connected devices
                foreach (var device_key in attached_devices.Keys)
                {
                    if (attached_devices[device_key].VendorId == REWIRE_PULSE_OXIMETER_VID && attached_devices[device_key].ProductId == REWIRE_PULSE_OXIMETER_PID)
                    {
                        _selected_usb_device = attached_devices[device_key];

                        //We found the correct device, so break out of the loop
                        break;
                    }
                }
            }

            return (_selected_usb_device != null);
        }

        public partial bool CheckPermissions()
        {
            if (_selected_usb_device != null && _usb_manager != null)
            {
                return (_usb_manager.HasPermission(_selected_usb_device));
            }
            else
            {
                return false;
            }
        }

        public partial void RequestPermissions()
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (_usb_manager != null && _selected_usb_device != null && current_activity != null)
            {
                //First check to see if the device matches the VID and PID of the ReWire pulse oximeter
                if (_selected_usb_device.VendorId == REWIRE_PULSE_OXIMETER_VID && _selected_usb_device.ProductId == REWIRE_PULSE_OXIMETER_PID)
                {
                    //Now let's check to see if we have already been granted permission to communicate with the device
                    if (!_usb_manager.HasPermission(_selected_usb_device))
                    {
                        //If permission has not been granted, we need to request permission
                        PendingIntent pending_intent = PendingIntent.GetBroadcast(current_activity, 0, new Android.Content.Intent(UsbReceiver.ACTION_USB_PERMISSION), PendingIntentFlags.Immutable);
                        IntentFilter intent_filter = new IntentFilter(UsbReceiver.ACTION_USB_PERMISSION);
                        current_activity.RegisterReceiver(_usb_receiver, intent_filter);
                        _usb_manager.RequestPermission(_selected_usb_device, pending_intent);
                    }
                }
            }
        }

        public partial void Connect()
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            //If we reach this function, then the device is connected and we also have permission
            //to interact with it.
            //So let's set everything up...
            if (_selected_usb_device != null && current_activity != null && _usb_receiver != null)
            {
                CdcAcmSerialDriver serial_driver = new CdcAcmSerialDriver(_selected_usb_device);
                _usb_device_connection = _usb_manager.OpenDevice(serial_driver.GetDevice());
                _serial_port = serial_driver.Ports.FirstOrDefault();

                if (_serial_port != null && _usb_device_connection != null)
                {
                    try
                    {
                        //Open the serial port and set up the connection
                        _serial_port.Open(_usb_device_connection);
                        _serial_port.SetParameters(115200, UsbSerialPort.DATABITS_8, StopBits.One, Parity.None);
                        _serial_port.SetDTR(true);

                        //List for "usb device detached" events in case the device gets detached
                        IntentFilter intent_filter = new IntentFilter();
                        intent_filter.AddAction(UsbManager.ActionUsbDeviceDetached);
                        current_activity.ApplicationContext.RegisterReceiver(_usb_receiver, intent_filter);
                    }
                    catch (Exception ex)
                    {
                        current_activity.UnregisterReceiver(_usb_receiver);
                        _serial_port = null;
                    }
                }
            }
        }

        public partial void Disconnect()
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (_usb_receiver != null && _selected_usb_device != null && current_activity != null)
            {
                current_activity.UnregisterReceiver(_usb_receiver);

                if (_serial_port != null)
                {
                    _serial_port.Close();
                    _serial_port = null;
                }
            }
        }

        public partial List<string> GetData()
        {
            List<string> result = new List<string>();

            try
            {
                if (_serial_port != null)
                {
                    byte[] current_read = new byte[1024];
                    int num_bytes_read = _serial_port.Read(current_read, 20);
                    if (num_bytes_read > 0)
                    {
                        var bytes_read = current_read.ToList().GetRange(0, num_bytes_read);
                        _buffer.AddRange(bytes_read);
                    }

                    if (_buffer.Contains(0x0A))
                    {
                        int index_of_newline = _buffer.IndexOf(0x0A);
                        var current_line_as_bytes = _buffer.Take(index_of_newline + 1);
                        string current_line = Encoding.UTF8.GetString(current_line_as_bytes.ToArray()).Trim();
                        if (_buffer.Count > (index_of_newline + 1))
                        {
                            _buffer = _buffer.Skip(index_of_newline + 1).ToList();
                        }
                        else
                        {
                            _buffer.Clear();
                        }

                        result.Add(current_line);
                    }
                }
            }
            catch (Exception ex)
            {
                //empty
            }

            return result;
        }

        public partial void SendStreamOnCommand()
        {
            if (_serial_port != null)
            {
                string stream_on_cmd = "stream on";
                var stream_on_cmd_bytes = Encoding.ASCII.GetBytes(stream_on_cmd);
                _serial_port.Write(stream_on_cmd_bytes, 0);
            }
        }

        public partial void SendVersionCommand()
        {
            if (_serial_port != null)
            {
                string stream_on_cmd = "version";
                var stream_on_cmd_bytes = Encoding.ASCII.GetBytes(stream_on_cmd);
                _serial_port.Write(stream_on_cmd_bytes, 0);
            }
        }

        #endregion
    }
}
