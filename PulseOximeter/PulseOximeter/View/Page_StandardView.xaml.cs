using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class Page_StandardView : ContentPage
{
	public Page_StandardView()
	{
		InitializeComponent();
		BindingContext = new Page_StandardView_ViewModel();
	}
}