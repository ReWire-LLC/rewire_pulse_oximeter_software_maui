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
            PlatformInitialization();
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

        private partial void PlatformInitialization();

        #endregion

        #region Public Partial Methods

        public partial void Open();

        public partial void Connect();

        public partial void Disconnect();

        public partial List<string> GetData();

        public partial void SendStreamOnCommand();

        #endregion
    }
}
