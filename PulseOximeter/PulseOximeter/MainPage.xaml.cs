using Plugin.Maui.Audio;
using PulseOximeter.Model.Audio;
using System.IO;

namespace PulseOximeter;

public partial class MainPage : ContentPage
{
    //private AudioDataChunk _sineToneChunk;
    private byte[] myWaveData;

    int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
		uint SAMPLE_FREQUENCY = 44100;
		ushort AUDIO_LENGTH_IN_SECONDS = 1;

        List<Byte> tempBytes = new List<byte>();

        WaveHeader header = new WaveHeader();
        FormatChunk format = new FormatChunk();
        DataChunk data = new DataChunk();

        // Create 1 second of tone at 697Hz
        SineGenerator leftData = new SineGenerator(697.0f,
           SAMPLE_FREQUENCY, AUDIO_LENGTH_IN_SECONDS);
        // Create 1 second of tone at 1209Hz
        SineGenerator rightData = new SineGenerator(1209.0f,
           SAMPLE_FREQUENCY, AUDIO_LENGTH_IN_SECONDS);

        data.AddSampleData(leftData.Data, rightData.Data);

        header.FileLength += format.Length() + data.Length();

        tempBytes.AddRange(header.GetBytes());
        tempBytes.AddRange(format.GetBytes());
        tempBytes.AddRange(data.GetBytes());

        myWaveData = tempBytes.ToArray();

        MemoryStream memoryStream = new MemoryStream(myWaveData);
        var audioPlayer = AudioManager.Current.CreatePlayer(memoryStream);
        audioPlayer.Play();
    }
}

