using PulseOximeter.Model;

namespace PulseOximeter;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		//Create the application model
		var application_model = ApplicationModel.GetInstance();

		//Start the background thread of the application model class
		application_model.Start();

		//Create the UI
		MainPage = new AppShell();
	}
}
