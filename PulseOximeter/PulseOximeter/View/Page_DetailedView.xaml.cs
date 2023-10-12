using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class Page_DetailedView : ContentPage
{
	public Page_DetailedView()
	{
		InitializeComponent();
		BindingContext = new Page_DetailedView_ViewModel();
	}
}