using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace wavetoy
{

    /// <summary>
    /// Holds the UI model along with the associated SignalGenerators
    /// </summary>
    public class Oscillator
    {
        public OscillatorOptions OsclliatorOptions { get; set; }
        public WaveTable WaveTable { get; set; }
    }

    /// <summary>
    /// Creates a wavetable filled with a sample rate of 44100, 1 channel, and the specified signal generator type
    /// Valid signal generator types are Sin, Square, Sawtooth, and Triangle
    /// </summary>
    public class WaveTable
    {
        public float Frequency { get; private set; }
        public float Amplitude { get; private set; }
        public SignalGeneratorType SignalGeneratorType { get; private set; }

        private float[] waveTable;
        private float sampleRate = 44100;
        private float readPosition = 0;

        public WaveTable(float frequency, float amplitude, SignalGeneratorType signalGeneratorType)
        {
            SetFrequency(frequency);
            SetAmplitude(amplitude);
            SetSignalGeneratorType(signalGeneratorType);    // setting this will fill the table
        }

        public void SetFrequency(float newFrequency)
        {
            if (newFrequency < 0)
                Frequency = 0;
            else
                Frequency = newFrequency;
        }

        public void SetAmplitude(float newAmplitude)
        {
            if (newAmplitude < 0)
                Amplitude = 0;
            else
                Amplitude = newAmplitude;
        }

        public void SetSignalGeneratorType(SignalGeneratorType signalGeneratorType)
        {
            if (SignalGeneratorType == signalGeneratorType)
                return;

            if (signalGeneratorType == SignalGeneratorType.Sin ||
                signalGeneratorType == SignalGeneratorType.Square ||
                signalGeneratorType == SignalGeneratorType.SawTooth ||
                signalGeneratorType == SignalGeneratorType.Triangle)
            {
                SignalGeneratorType = signalGeneratorType;
                FillTable(signalGeneratorType);
            }
            else
            {
                throw new NotSupportedException("Unsupported SignalGeneratorType");
            }
        }

        private void FillTable(SignalGeneratorType signalGeneratorType)
        {
            waveTable = new float[(int)sampleRate];
            var sigGen = new SignalGenerator() { Frequency = 2, Gain = 1, Type = signalGeneratorType };
            sigGen.Read(waveTable, 0, (int)sampleRate);

            // debug
            //foreach (var value in waveTable)
                //System.Diagnostics.Debug.WriteLine(value.ToString());

            readPosition = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[i] = waveTable[(int)readPosition] * Amplitude;
                readPosition += Frequency;
                while (readPosition >= sampleRate)
                    readPosition -= sampleRate;
            }

            return count;
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
                SignalGeneratorType sigGenType;
                if (!Enum.TryParse(oscOptions.WaveType, out sigGenType))    // if we fail to parse, skip
                    continue;

                // TODO add back in the ability to have white and pink noise
                var waveTable = new WaveTable(oscOptions.Frequency, oscOptions.Amplitude, sigGenType);
                oscillators.Add(new Oscillator() { OsclliatorOptions = oscOptions, WaveTable = waveTable });
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
                    if (!osc.OsclliatorOptions.Enabled)
                        continue;

                    // the first one is the output signal
                    if (j == 0)
                    {
                        // read data from the first oscillator
                        osc.WaveTable.Read(tempBuffer, 0, 1);
                        buffer[i] = tempBuffer[0];
                        uiCallbackBuffer[i] = tempBuffer[0];
                    }
                    else // else we are going to mod the n-1 oscilator
                    {
                        var prevOsc = oscillators[j - 1];
                        osc.WaveTable.Read(tempBuffer, 0, 1);

                        // TODO, figure out how to not be bound to arbitrary UI strings (bind to enums?)
                        if (osc.OsclliatorOptions.ModType == "Frequency")
                            prevOsc.WaveTable.SetFrequency(prevOsc.OsclliatorOptions.Frequency + tempBuffer[0]);
                        else if (osc.OsclliatorOptions.ModType == "Amplitude")
                            prevOsc.WaveTable.SetAmplitude(prevOsc.OsclliatorOptions.Amplitude + tempBuffer[0]);
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
            foreach (var osc in oscillators)
            {
                // if the oscillator isn't enabled, skip it
                if (!osc.OsclliatorOptions.Enabled)
                    continue;

                if (osc.WaveTable.SignalGeneratorType.ToString() != osc.OsclliatorOptions.WaveType)
                {
                    SignalGeneratorType sigGenType;
                    if (!Enum.TryParse(osc.OsclliatorOptions.WaveType, out sigGenType))    // if we fail to parse, skip
                        continue;

                    osc.WaveTable.SetSignalGeneratorType(sigGenType);
                }

                if (osc.WaveTable.Frequency != osc.OsclliatorOptions.Frequency)
                    osc.WaveTable.SetFrequency(osc.OsclliatorOptions.Frequency);

                if (osc.WaveTable.Amplitude != osc.OsclliatorOptions.Amplitude)
                    osc.WaveTable.SetAmplitude(osc.OsclliatorOptions.Amplitude);
            }
        }
    }
}
