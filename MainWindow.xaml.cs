using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace wavetoy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // the engine that creates and plays the waves
        private WaveEngine waveEngine;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            // create the oscillator option models
            var osc1 = new OscillatorOptions() { Name = "Main Oscillator", Enabled = false, ModType = "Frequency", WaveType = "Sin", Frequency = 400, Amplitude = 0.2f };
            var osc2 = new OscillatorOptions() { Name = "1st Modulator", Enabled = false, ModType = "Frequency", WaveType = "Sin", Frequency = 400, Amplitude = 0 };
            var osc3 = new OscillatorOptions() { Name = "2nd Modulator", Enabled = false, ModType = "Frequency", WaveType = "Sin", Frequency = 400, Amplitude = 0 };

            // bind them to the controls
            OscillatorControl1.DataContext = osc1;
            OscillatorControl2.DataContext = osc2;
            OscillatorControl3.DataContext = osc3;

            // disable the modtype combobox on the first osc control and change the max value
            OscillatorControl1.ModTypeLabel.Visibility = Visibility.Hidden;
            OscillatorControl1.ModTypeComboBox.Visibility = Visibility.Hidden;
            OscillatorControl1.AmpSlider.Maximum = 1;

            List<OscillatorOptions> oscillatorOptions = new List<OscillatorOptions>() { osc1, osc2, osc3 };
            waveEngine = new WaveEngine(oscillatorOptions);
            waveEngine.SampleProviderUpdate += WaveEngine_SampleProviderUpdate;
        }

        /// <summary>
        /// The callback for the engine to update the UI, dispatches the graph draw on the UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveEngine_SampleProviderUpdate(object sender, float[] e)
        {
            try { Application.Current.Dispatcher.Invoke(() => GraphWaveform(e)); } catch { } // sometimes it fires after dispose
        }

        /// <summary>
        /// The collection that is bound to the PolyLine that draws the waveform on the UI
        /// </summary>
        private PointCollection points = new PointCollection();
        public PointCollection Points
        {
            get { return points; }
            set
            {
                points = value;
                NotifyPropertyChanged("Points");
            }
        }

        /// <summary>
        /// Takes the waveform, scales it, and generates the points for the point collection
        /// </summary>
        private void GraphWaveform(float[] data)
        {
            double canvasHeight = WaveCanvas.ActualHeight;
            double canvasWidth = WaveCanvas.ActualWidth;

            // TODO: we discard all the data except the first "few" points, is there a better way to do this?
            int observablePoints = 1024;

            double xScale = canvasWidth / observablePoints;
            double yScale = canvasHeight / 2;

            var pointCollection = new PointCollection();

            for (int i = 0; i < observablePoints; i++)
                pointCollection.Add(new Point(i * xScale, (canvasHeight / 2) - (data[i] * yScale)));

            this.Points = pointCollection;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            waveEngine.Play();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            waveEngine.Stop();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            App.Current.MainWindow.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
