using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.CrossPlatform
{
    public partial class FolderManager_CrossPlatform
    {
        #region Events

        public event EventHandler FolderSelected;

        #endregion

        #region Singleton

        private static volatile FolderManager_CrossPlatform _instance = null;
        private static object _instance_lock = new object();

        private FolderManager_CrossPlatform()
        {
            //empty
        }

        public static FolderManager_CrossPlatform GetInstance()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FolderManager_CrossPlatform();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Platform-specific methods (these will be defined by each platform

        public partial void RemoveFolderPermissions();

        public partial void RequestSelectFolder();

        public partial string GetSelectedFolderUriString();

        public partial bool HasFolderBeenSelectedAndPermissionsGiven();

        public partial Stream OpenFileForReading(string file_name);

        public partial Stream OpenFileForWriting(string file_name);

        #endregion

        #region Public methods

        public void NotifyFolderSelected()
        {
            FolderSelected?.Invoke(this, new EventArgs());
        }

        #endregion
    }
}
