﻿using System;
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

        [Description("Searching for device")]
        SearchingForDevice,

        [Description("Requesting permission")]
        RequestingPermission,

        [Description("Connecting to device")]
        ConnectingToDevice,

        [Description("Requesting firmware version")]
        Connected_RequestFirmwareVersion,

        [Description("Device connected")]
        Connected,

        [Description("The application has encountered an error. Please re-start.")]
        Error
    }
}
