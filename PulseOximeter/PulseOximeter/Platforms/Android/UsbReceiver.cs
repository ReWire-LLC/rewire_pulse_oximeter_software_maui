using Android.Content;
using Android.Hardware.Usb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.CrossPlatform
{
    public class UsbReceiver : BroadcastReceiver
    {
        #region Events

        public event EventHandler<UsbDevice> PermissionGranted;
        public event EventHandler<UsbDevice> DeviceAttached;
        public event EventHandler<UsbDevice> DeviceDetached;

        #endregion

        #region Private members

        public static string ACTION_USB_PERMISSION = "USB_PERMISSION";

        #endregion

        #region Singleton

        private static volatile UsbReceiver _instance = null;
        private static object _instance_lock = new object();

        private UsbReceiver()
        {
            //empty
        }

        public static UsbReceiver GetInstance ()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UsbReceiver();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region BroadcastReceiver implementation

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            lock (this)
            {
                UsbDevice device;
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                {
                    device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice, Java.Lang.Class.FromType(typeof(UsbDevice)));
                }
                else
                {
                    device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                }

                if (device != null)
                {
                    if (ACTION_USB_PERMISSION.Equals(action))
                    {
                        if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                        {
                            PermissionGranted?.Invoke(this, device);
                        }
                    }
                    else if (UsbManager.ActionUsbDeviceAttached.Equals(action))
                    {
                        DeviceAttached?.Invoke(this, device);
                    }
                    else if (UsbManager.ActionUsbDeviceDetached.Equals(action))
                    {
                        DeviceDetached?.Invoke(this, device);
                    }
                }

            }
        }

        #endregion
    }
}
