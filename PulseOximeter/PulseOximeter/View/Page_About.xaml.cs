using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class Page_About : ContentPage
{
	public Page_About()
	{
		InitializeComponent();
		BindingContext = new Page_About_ViewModel();
	}
}