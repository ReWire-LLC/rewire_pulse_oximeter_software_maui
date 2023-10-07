using Android.Content;
using Android.Webkit;
using AndroidX.DocumentFile.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.CrossPlatform
{
    public partial class FolderManager_CrossPlatform
    {
        #region Public data members

        public int REQUEST_TREE = 85;

        #endregion

        #region Implementation of platform-specific methods

        public partial void RemoveFolderPermissions()
        {
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (current_activity != null)
            {
                //Get the list of persistable permissions that have been granted to this activity
                List<UriPermission> permissions = current_activity.ContentResolver.PersistedUriPermissions.ToList();

                //Remove the permissions to access this folder
                current_activity.ContentResolver.ReleasePersistableUriPermission(permissions[0].Uri, (Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.GrantWriteUriPermission));
            }
        }

        public partial void RequestSelectFolder()
        {
            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (current_activity != null)
            {
                //If we successfully retrieved the current activity, then create an Android intent
                //and open the system "file picker". This will allow the user to select a folder
                //that will be used by this application in the future. The application will receive
                //persistable permissions to read/write from/to this folder.
                var intent = new Android.Content.Intent(Android.Content.Intent.ActionOpenDocumentTree);
                intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission |
                                Android.Content.ActivityFlags.GrantWriteUriPermission |
                                Android.Content.ActivityFlags.GrantPersistableUriPermission |
                                Android.Content.ActivityFlags.GrantPrefixUriPermission);

                //Start the file picker
                current_activity.StartActivityForResult(intent, REQUEST_TREE);
            }
        }

        public partial string GetSelectedFolderUriString()
        {
            string result = string.Empty;

            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (current_activity != null)
            {
                //Get the list of persistable permissions that our application has
                List<UriPermission> permissions = current_activity.ContentResolver.PersistedUriPermissions.ToList();
                if (permissions != null && permissions.Count > 0)
                {
                    //Grab the first persistable permission, and then use it to grab the xnerve folder
                    DocumentFile xnerve_folder = DocumentFile.FromTreeUri(current_activity, permissions[0].Uri);
                    result = xnerve_folder.Name.ToString();
                }
            }

            return result;
        }

        public partial bool HasFolderBeenSelectedAndPermissionsGiven()
        {
            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (current_activity != null)
            {
                //Get the list of persistable permissions that have been granted to this activity
                List<UriPermission> permissions = current_activity.ContentResolver.PersistedUriPermissions.ToList();

                //If the list of permissions is not null or empty, then we return true to caller
                return (permissions != null && permissions.Count > 0);
            }
            else
            {
                //If we weren't able to retrieve the current activity, then return false
                return false;
            }
        }

        public partial Stream OpenFileForReading(string file_name)
        {
            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (current_activity != null)
            {
                //Get the list of persistable permissions that our application has
                List<UriPermission> permissions = current_activity.ContentResolver.PersistedUriPermissions.ToList();
                if (permissions != null && permissions.Count > 0)
                {
                    //Grab the first persistable permission, and then use it to grab the xnerve folder
                    DocumentFile xnerve_folder = DocumentFile.FromTreeUri(current_activity, permissions[0].Uri);

                    //Within the xnerve folder, find the file that the caller has requested
                    var file = xnerve_folder.FindFile(file_name);
                    if (file != null)
                    {
                        //Open an input stream to read the configuration file
                        var input_stream = current_activity.ContentResolver.OpenInputStream(file.Uri);

                        //Return the input stream to the caller
                        return input_stream;
                    }
                }
            }

            return null;
        }

        public partial Stream OpenFileForWriting(string file_name)
        {
            //Get the current activity
            var current_activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (current_activity != null)
            {
                //Get the list of persistable permissions that our application has
                List<UriPermission> permissions = current_activity.ContentResolver.PersistedUriPermissions.ToList();
                if (permissions != null && permissions.Count > 0)
                {
                    //Grab the first persistable permission, and then use it to grab the xnerve folder
                    DocumentFile xnerve_folder = DocumentFile.FromTreeUri(current_activity, permissions[0].Uri);

                    string mime_type = string.Empty;
                    string file_extension = MimeTypeMap.GetFileExtensionFromUrl(file_name);
                    if (!string.IsNullOrEmpty(file_extension))
                    {
                        //Get the mime type for the file extension
                        mime_type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(file_extension);

                        //Create the file
                        DocumentFile output_file = xnerve_folder.CreateFile(mime_type, file_name);

                        //Open an output stream to write to the file
                        if (output_file != null)
                        {
                            var output_stream = current_activity.ContentResolver.OpenOutputStream(output_file.Uri);

                            //Return the input stream to the caller
                            return output_stream;
                        }
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
