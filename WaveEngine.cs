using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace wavetoy
{
    /// <summary>
    /// The main controller for the audio output and WaveMixer
    /// </summary>
    public class WaveEngine
    {
        /// <summary>
        /// Passes the UI callback into the underlying WaveMixer,
        /// which will provide sample data for display
        /// </summary>
        public event EventHandler<float[]> SampleProviderUpdate
        {
            add { waveMixer.SampleProviderUpdate += value; }
            remove { waveMixer.SampleProviderUpdate -= value; }
        }

        private WaveOutEvent waveOut;
        private WaveGenerator waveMixer;

        /// <summary>
        /// Initializes a new instance of the WaveEngine class
        /// </summary>
        /// <param name="oscillatorOptions"></param>
        public WaveEngine(List<OscillatorOptions> oscillatorOptions)
        {
            waveOut = new WaveOutEvent();
            waveMixer = new WaveGenerator(oscillatorOptions);
        }

        // Don't forget to release your unmanaged resources
        ~WaveEngine()
        {
            waveOut.Dispose();
        }

        /// <summary>
        /// Intializes the required resources and starts playback
        /// </summary>
        public void Play()
        {
            // debug
            //waveOut.Init(new SignalGenerator() { Gain = 0.1, Frequency = 100, Type = SignalGeneratorType.Sin });

            if (waveOut.PlaybackState == PlaybackState.Playing)
                Stop();

            waveOut.Init(waveMixer);
            waveOut.Volume = 0.2f;
            waveOut.Play();
        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            waveOut.Stop();
        }
    }

    /// <summary>
    /// Given a list of OscillatorOptions, this class creates the underlying
    /// SignalGenerators, updates the paramters when the UI changes the options,
    /// mixes the outputs from SignalGenerators and outputs the final waveform
    /// </summary>
    public class WaveGenerator : ISampleProvider
    {
        public WaveFormat WaveFormat { get; private set; }
        public event EventHandler<float[]> SampleProviderUpdate;    // feeds wave data to the UI

        // list of the OscillatorOptions, these are the Models that are bound to the UI
        // when the UI changes, so will these Models
        private List<OscillatorOptions> oscillatorOptions;

        // list of Oscillators, this contains both the options Model that is
        // bound to the UI and the signal generator associated with it
        private List<Oscillator> oscillators = new List<Oscillator>();

        /// <summary>
        /// Initializes a new instance of the WaveGenerator class
        /// </summary>
        /// <param name="oscillatorOptions"></param>
        public WaveGenerator(List<OscillatorOptions> oscillatorOptions)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            this.oscillatorOptions = oscillatorOptions;

            // build the oscillator list
            foreach (var oscOptions in oscillatorOptions)
            {
                var sigGen = new SignalGenerator(WaveFormat.SampleRate, WaveFormat.Channels);
                oscillators.Add(new Oscillator() { osclliatorOptions = oscOptions, signalGenerator = sigGen });
            }

            UpdateSignalGenerators();
        }

        /// <summary>
        /// Takes all of the enabled oscillators and generates the final waveform
        /// The first oscillator is the main output signal, each oscillator in turn
        /// modulates the previous oscillator (n-1)
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            // first update the signal generators with any UI changes
            UpdateSignalGenerators();

            // copy the data into a buffer to give to the UI
            var uiCallbackBuffer = new float[count];
            var tempBuffer = new float[1];

            // for each requested sample
            for (int i = 0; i < count; i++)
            {
                // for each oscillator
                for (int j = 0; j < oscillators.Count; j++)
                {
                    var osc = oscillators[j];

                    //if the osclilator isn't enabled, skip it
                    if (!osc.osclliatorOptions.Enabled)
                        continue;

                    // the first one is the output signal
                    if (j == 0)
                    {
                        // read data from the first oscillator
                        osc.signalGenerator.Read(tempBuffer, 0, 1);
                        buffer[i] = tempBuffer[0];
                        uiCallbackBuffer[i] = tempBuffer[0];
                    }
                    else // else we are going to mod the n-1 oscilator
                    {
                        var prevOsc = oscillators[j - 1];
                        osc.signalGenerator.Read(tempBuffer, 0, 1);

                        // TODO, figure out how to not be bound to arbitrary UI strings (bind to enums?)
                        if (osc.osclliatorOptions.ModType == "Frequency")
                            prevOsc.signalGenerator.Frequency = prevOsc.osclliatorOptions.Frequency + tempBuffer[0];
                        else if (osc.osclliatorOptions.ModType == "Amplitude")
                            prevOsc.signalGenerator.Gain = prevOsc.osclliatorOptions.Amplitude + tempBuffer[0];
                    }
                }
            }

            // give the UI the data
            if (SampleProviderUpdate != null)
                SampleProviderUpdate(null, uiCallbackBuffer);

            return count;
        }

        /// <summary>
        /// Modifies the SignalGenerators with UI changes
        /// </summary>
        private void UpdateSignalGenerators()
        {
            foreach(var osc in oscillators)
            {
                // if the oscillator isn't enabled, skip it
                if (!osc.osclliatorOptions.Enabled)
                    continue;

                if (osc.signalGenerator.Type.ToString() != osc.osclliatorOptions.WaveType)
                {
                    SignalGeneratorType sigGenType;
                    if (!Enum.TryParse(osc.osclliatorOptions.WaveType, out sigGenType))    // if we fail to parse, skip
                        continue;

                    osc.signalGenerator.Type = sigGenType;
                }

                if (osc.signalGenerator.Frequency != osc.osclliatorOptions.Frequency)
                    osc.signalGenerator.Frequency = osc.osclliatorOptions.Frequency;

                if (osc.signalGenerator.Gain != osc.osclliatorOptions.Amplitude)
                    osc.signalGenerator.Gain = osc.osclliatorOptions.Amplitude;
            }
        }
    }

    /// <summary>
    /// The model that is bound to the UI and passes the UI changes to the engine
    /// Contains all the original values for the oscillators to reference as they modualte
    /// </summary>
    public class OscillatorOptions
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public string WaveType { get; set; }
        public string ModType { get; set; }
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
    }

    /// <summary>
    /// Holds the UI model along with the associated SignalGenerators
    /// </summary>
    public class Oscillator
    {
        public OscillatorOptions osclliatorOptions { get; set; }
        public SignalGenerator signalGenerator { get; set; }
    }
}
