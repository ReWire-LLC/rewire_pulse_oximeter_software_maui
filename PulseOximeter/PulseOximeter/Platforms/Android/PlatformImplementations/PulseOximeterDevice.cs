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

        private UsbDevice _connected_usb_device = null;
        private UsbDeviceConnection _usb_device_connection = null;
        private UsbSerialPort _serial_port = null;

        private List<byte> input_bytes = new List<byte>();
        private byte end_of_line_byte = 0x0A;

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
            OnPermissionGranted(e);
        }

        private void Handle_DeviceDetached(object sender, global::Android.Hardware.Usb.UsbDevice e)
        {
            OnDeviceDetached(e);
        }

        private void Handle_DeviceAttached(object sender, global::Android.Hardware.Usb.UsbDevice e)
        {
            OnDeviceAttached(e);
        }

        #endregion

        #region Private Methods

        private void OnPermissionGranted (UsbDevice usb_device)
        {
            //Set the "connected" USB device to be the device passed in as a parameter to this function
            _connected_usb_device = usb_device;

            //Connect to the device
            Connect();
        }

        private void OnDeviceAttached (UsbDevice usb_device)
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (_usb_manager != null && usb_device != null && current_activity != null)
            {
                //First check to see if the device matches the VID and PID of the ReWire pulse oximeter
                if (usb_device.VendorId == REWIRE_PULSE_OXIMETER_VID && usb_device.ProductId == REWIRE_PULSE_OXIMETER_PID)
                {
                    //Now let's check to see if we have already been granted permission to communicate with the device
                    if (_usb_manager.HasPermission(usb_device))
                    {
                        //If so, then call the function that handles what happens after permission has been granted
                        OnPermissionGranted(usb_device);
                    }
                    else
                    {
                        //If permission has not been granted, we need to request permission
                        PendingIntent pending_intent = PendingIntent.GetBroadcast(current_activity, 0, new Android.Content.Intent(UsbReceiver.ACTION_USB_PERMISSION), PendingIntentFlags.Immutable);
                        IntentFilter intent_filter = new IntentFilter(UsbReceiver.ACTION_USB_PERMISSION);
                        current_activity.RegisterReceiver(_usb_receiver, intent_filter);
                        _usb_manager.RequestPermission(usb_device, pending_intent);
                    }
                }
            }
        }

        private void OnDeviceDetached (UsbDevice usb_device)
        {
            try
            {
                if (usb_device != null)
                {
                    if (usb_device.VendorId == REWIRE_PULSE_OXIMETER_VID && usb_device.ProductId == REWIRE_PULSE_OXIMETER_PID)
                    {
                        Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                //empty
            }
        }

        private void SelectPulseOximeterDevice_FromAllConnectedDevices ()
        {
            //If the "connected usb device" object is currently null...
            if (_connected_usb_device == null && _usb_manager != null)
            {
                //Grab the Android USB manager and get a list of connected devices
                var attached_devices = _usb_manager.DeviceList;

                //Find the pulse oximeter in the list of connected devices
                foreach (var device_key in attached_devices.Keys)
                {
                    if (attached_devices[device_key].VendorId == REWIRE_PULSE_OXIMETER_VID && attached_devices[device_key].ProductId == REWIRE_PULSE_OXIMETER_PID)
                    {
                        _connected_usb_device = attached_devices[device_key];
                        
                        //We found the correct device, so break out of the loop
                        break;
                    }
                }
            }
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

                if (_usb_manager != null)
                {
                    //Subscribe to notifications from the USB receiver class
                    _usb_receiver.DeviceDetached += Handle_DeviceAttached;
                    _usb_receiver.DeviceDetached += Handle_DeviceDetached;
                    _usb_receiver.PermissionGranted += Handle_PermissionGranted;
                }
            }

            return (current_activity != null && _usb_manager != null);
        }

        public partial void Open()
        {
            SelectPulseOximeterDevice_FromAllConnectedDevices();

            if (_connected_usb_device != null)
            {
                OnDeviceAttached(_connected_usb_device);
            }
        }

        public partial void Connect()
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            //If we reach this function, then the device is connected and we also have permission
            //to interact with it.
            //So let's set everything up...
            if (_connected_usb_device != null && current_activity != null && _usb_receiver != null)
            {
                CdcAcmSerialDriver serial_driver = new CdcAcmSerialDriver(_connected_usb_device);
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

            if (_usb_receiver != null && _connected_usb_device != null && current_activity != null)
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
                    byte[] buffer = new byte[1024];
                    int num_bytes_read = _serial_port.Read(buffer, 20);
                    if (num_bytes_read > 0)
                    {
                        var bytes_read = buffer.ToList().GetRange(0, num_bytes_read);
                        input_bytes.AddRange(bytes_read);
                        int index_of_last_newline = input_bytes.FindLastIndex(x => x == end_of_line_byte);
                        if (index_of_last_newline > -1)
                        {
                            var subset_of_bytes = input_bytes.GetRange(0, index_of_last_newline + 1);
                            input_bytes = input_bytes.Skip(index_of_last_newline + 1).ToList();

                            var string_data = System.Text.Encoding.ASCII.GetString(subset_of_bytes.ToArray()).Trim();
                            result = string_data.Split('\n').ToList();
                        }
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

        #endregion
    }
}
