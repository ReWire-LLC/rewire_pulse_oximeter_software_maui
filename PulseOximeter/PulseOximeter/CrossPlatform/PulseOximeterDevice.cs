using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.CrossPlatform
{
    public partial class PulseOximeterDevice
    {
        #region Private constants

        private const int REWIRE_PULSE_OXIMETER_VID = 0x04D8;
        private const int REWIRE_PULSE_OXIMETER_PID = 0xE636;

        #endregion

        #region Private data members



        #endregion

        #region Constructor

        public PulseOximeterDevice()
        {
            //empty
        }

        #endregion

        #region Public Properties

        public bool IsConnected
        {
            get
            {
                return IsConnectedAndStreaming();
            }
        }

        #endregion

        #region Private Partial Methods

        private partial bool IsConnectedAndStreaming();

        #endregion

        #region Public Partial Methods

        public partial bool PlatformInitialization();

        public partial bool ScanForDevice();

        public partial bool CheckPermissions();

        public partial void RequestPermissions();

        public partial void Connect();

        public partial void Disconnect();

        public partial List<string> GetData();

        public partial void SendStreamOnCommand();

        public partial void SendVersionCommand();

        #endregion
    }
}
