using PulseOximeter.Model;
using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.ViewModel
{
    public class Page_About_ViewModel : NotifyPropertyChangedObject
    {
        #region Constructor

        public Page_About_ViewModel()
        {
            //empty
        }

        #endregion

        #region Properties

        public string ApplicationVersion
        {
            get
            {
                return (AppInfo.VersionString + " (" + AppInfo.BuildString + ")");
            }
        }

        public string ApplicationBuildDate
        {
            get
            {
                return ApplicationConfiguration.GetBuildDate().ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        #endregion
    }
}
