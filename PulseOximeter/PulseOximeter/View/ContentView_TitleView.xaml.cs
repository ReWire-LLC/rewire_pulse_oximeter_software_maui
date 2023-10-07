using PulseOximeter.CrossPlatform;
using PulseOximeter.Model;
using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class ContentView_TitleView : ContentView
{
	public ContentView_TitleView()
	{
		InitializeComponent();
        BindingContext = new ContentView_TitleView_ViewModel();
	}

    private Page GetParentPage(Element btn)
    {
        bool done = false;
        Element parent = btn;
        while (!done)
        {
            parent = parent.Parent;
            if (parent == null || parent is Page)
            {
                done = true;
            }
        }

        return (parent as Page);
    }

    private void MuteButton_Clicked(object sender, EventArgs e)
    {
        var vm = this.BindingContext as ContentView_TitleView_ViewModel;
        if (vm != null )
        {
            vm.ToggleMute();
        }
    }

    private async void RecordButton_Clicked(object sender, EventArgs e)
    {
        var vm = this.BindingContext as ContentView_TitleView_ViewModel;
        if (vm != null)
        {
            //Check to see if we are currently recording
            if (vm.IsRecording)
            {
                //If so, then stop the current recording
                vm.StopRecording();
            }
            else
            {
                //If we are not currently recording...

                //Check to see if a folder has been selected by the user as a location
                //where we can stored recording files
                if (FolderManager_CrossPlatform.GetInstance().HasFolderBeenSelectedAndPermissionsGiven())
                {
                    //If we already have permissions for a folder, then we just need to initiate the recording
                    InitiateRecording();
                }
                else
                {
                    //If we don't yet have permissions for a folder, then we need to ask the user for permissions
                    //for a folder
                    var current_page = GetParentPage(sender as Element);
                    if (current_page != null)
                    {
                        var result = await current_page.DisplayAlert("Select a save location", "Please select a folder on your device where pulse oximeter recordings will be stored.", "OK", "Cancel");
                        if (result)
                        {
                            //If the user pressed "OK"

                            //Request permissions for a folder
                            FolderManager_CrossPlatform.GetInstance().FolderSelected += Handle_FolderSelected;
                            FolderManager_CrossPlatform.GetInstance().RequestSelectFolder();
                        }
                    }
                }
            }
        }
    }

    private void Handle_FolderSelected(object sender, EventArgs e)
    {
        //If this code is reached, then the user has selected a folder where we will
        //store pulse oximeter recordings
        if (FolderManager_CrossPlatform.GetInstance().HasFolderBeenSelectedAndPermissionsGiven())
        {
            InitiateRecording();
        }
    }

    private void InitiateRecording ()
    {
        //Create a default file name
        string file_datetime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string file_name = "ReWire_PulseOximeter_Recording_" + file_datetime + ".csv";
        var vm = this.BindingContext as ContentView_TitleView_ViewModel;
        if (vm != null)
        {
            vm.StartRecording(file_name);
        }
    }
}
