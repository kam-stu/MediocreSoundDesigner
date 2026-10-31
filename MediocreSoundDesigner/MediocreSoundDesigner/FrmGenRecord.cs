using MediocreSoundDesigner.Properties;
using NAudio.Wave;

namespace MediocreSoundDesigner;

public partial class FrmGenRecord : FrmEditMaster
{
    private WaveIn waveSource = null;
    private List<float> recordedSamples;
    private bool isRecording;
    private int targetChannels = 1;

    public FrmGenRecord()
    {
        InitializeComponent();
    }

    protected override void Init()
    {
        base.Init();
        recordedSamples = new();
        isRecording = false;
        btnRecord.BackgroundImage = Resources.record;
    }

    protected override void Apply()
    {
        track.ReplaceSamples(recordedSamples.ToArray());
    }

    private void btnRecord_Click(object sender, EventArgs e)
    {
        if (isRecording)
        {
            waveSource?.StopRecording();
            isRecording = false;
            btnRecord.BackgroundImage = Resources.record;
        }
        else
        {
            try
            {
                isRecording = true;
                recordedSamples.Clear();
                btnRecord.BackgroundImage = Resources.stop;

                // Default to standard 44.1 kHz mono
                int sampleRate = 44100;
                int inputChannels = 1;

                waveSource = new WaveIn
                {
                    WaveFormat = new WaveFormat(sampleRate, 16, inputChannels)
                };

                waveSource.DataAvailable += WaveSource_DataAvailable;
                waveSource.RecordingStopped += WaveSource_RecordingStopped;

                waveSource.StartRecording();
            }
            catch (Exception ex)
            {
                isRecording = false;
                btnRecord.BackgroundImage = Resources.record;
                MessageBox.Show($"Failed to start recording: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
    {
        int bytesPerSample = waveSource.WaveFormat.BitsPerSample / 8;
        int channels = waveSource.WaveFormat.Channels;
        int frameSize = bytesPerSample * channels;
        int sampleCount = e.BytesRecorded / frameSize;

        for (int i = 0; i < sampleCount; i++)
        {
            int offset = i * frameSize;
            short sample16 = BitConverter.ToInt16(e.Buffer, offset);
            float floatSample = sample16 / 32768f;

            recordedSamples.Add(floatSample);

            // If the target playback track is stereo, duplicate sample to prevent 2x playback speed
            if (targetChannels == 2)
            {
                recordedSamples.Add(floatSample);
            }
        }
    }

    // Executed automatically after StopRecording() has completed
    private void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
    {
        if (waveSource != null)
        {
            waveSource.Dispose();
            waveSource = null;
        }

        MessageBox.Show("Recording saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
