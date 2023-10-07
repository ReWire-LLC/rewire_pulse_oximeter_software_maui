using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using PulseOximeter.CrossPlatform;

namespace PulseOximeter;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (resultCode == Result.Ok)
        {
            if (requestCode == FolderManager_CrossPlatform.GetInstance().REQUEST_TREE)
            {
                // The result data contains a URI for the document or directory that
                // the user selected.
                Android.Net.Uri uri = null;
                if (data != null)
                {
                    uri = data.Data;
                    var flags = data.Flags & (Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.GrantWriteUriPermission);

                    //Take the persistable URI permissions (so that they actually persist)
                    this.ContentResolver.TakePersistableUriPermission(uri, flags);

                    //Perform operations on the document using its URI.
                    FolderManager_CrossPlatform.GetInstance().NotifyFolderSelected();
                }
            }
        }
    }
}
