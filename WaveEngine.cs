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
}
