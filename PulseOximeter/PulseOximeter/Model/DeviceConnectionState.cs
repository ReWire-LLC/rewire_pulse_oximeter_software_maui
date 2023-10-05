using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public enum DeviceConnectionState
    {
        [Description("Waiting for OS to be ready")]
        WaitForOperatingSystemToBeReady,

        [Description("No device found")]
        NoDevice,

        [Description("Device connected")]
        Connected,

        [Description("The application has encountered an error. Please re-start.")]
        Error
    }
}
