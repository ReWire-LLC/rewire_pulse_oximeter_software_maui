using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class Page_StandardView : ContentPage
{
	public Page_StandardView()
	{
		InitializeComponent();
		BindingContext = new MainPageViewModel();
	}

    private void ConnectButton_Clicked(object sender, EventArgs e)
    {
		var vm = this.BindingContext as MainPageViewModel;
		if (vm != null)
		{
			vm.Connect();
		}
    }
}