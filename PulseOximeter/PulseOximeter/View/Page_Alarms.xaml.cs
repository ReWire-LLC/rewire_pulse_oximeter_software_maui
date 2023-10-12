using PulseOximeter.ViewModel;

namespace PulseOximeter.View;

public partial class Page_Alarms : ContentPage
{
	public Page_Alarms()
	{
		InitializeComponent();
        this.BindingContext = new Page_SetAlarms_ViewModel();
	}

    private void ApplyButton_Clicked(object sender, EventArgs e)
    {
        var vm = this.BindingContext as Page_SetAlarms_ViewModel;
        if (vm != null)
        {
            //Get the settings entered by the user
            var hr_min_string = HeartRateMinimumAlarmTextBox.Text;
            var hr_max_string = HeartRateMaximumAlarmTextBox.Text;
            var spo2_min_string = SpO2MinimumAlarmTextBox.Text;
            var spo2_max_string = SpO2MaximumAlarmTextBox.Text;

            //Convert the values into integers
            bool parse_success_hr_min = Int32.TryParse(hr_min_string, out int hr_min);
            bool parse_success_hr_max = Int32.TryParse(hr_max_string, out int hr_max);
            bool parse_success_spo2_min = Int32.TryParse(spo2_min_string, out int spo2_min);
            bool parse_success_spo2_max = Int32.TryParse(spo2_max_string, out int spo2_max);

            if (parse_success_hr_min && parse_success_hr_max && parse_success_spo2_min && parse_success_spo2_max)
            {
                vm.ApplyAlarmSettings(hr_min, hr_max, spo2_min, spo2_max);
                ErrorMessageTextBlock.Text = string.Empty;
            }
            else
            {
                ErrorMessageTextBlock.Text = "You must enter valid integer values. Please correct your entries and try again.";
            }
        }
    }

    private void HeartRateMinimumAlarmTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErrorMessageTextBlock.Text = "Your changes are not saved until you click the Apply button.";
    }

    private void SpO2MinimumAlarmTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErrorMessageTextBlock.Text = "Your changes are not saved until you click the Apply button.";
    }

    private void HeartRateMaximumAlarmTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErrorMessageTextBlock.Text = "Your changes are not saved until you click the Apply button.";
    }

    private void SpO2MaximumAlarmTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErrorMessageTextBlock.Text = "Your changes are not saved until you click the Apply button.";
    }
}