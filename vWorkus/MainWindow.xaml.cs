using System;
using System.Globalization;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using NAudio.Wave;
using vWorkus.Properties;
using Timer = System.Timers.Timer;

namespace vWorkus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Timer _aTimer;
        private DateTime _endTime;
        private int _step;
        private string _pathToAlertFile;

        private TimeSpan Remaining => _endTime - DateTime.Now;

        private DateTime StartTime { get; set; }
        

        public MainWindow()
        {
            StartTime = DateTime.Now;

            InitializeComponent();

            PrepareSettings();
            SetTimer();
        }

        private void PrepareSettings()
        {
            const int MINUTE_TO_MS = 1000 * 60;

            _endTime = GetTotalTimeFromSettings(Settings.Default.TotalTime);
            _step = Settings.Default.StepInMinutes * MINUTE_TO_MS;
            _pathToAlertFile = Settings.Default.AlertSoundPath;

            Dispatcher.Invoke(() =>
            {
                LbCountdown.Content = GetRemainingInString();
            });
        }

        private void SetTimer()
        {
            _aTimer = new Timer(_step);

            _aTimer.Elapsed += OnTimedEvent;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LbCountdown.Content = GetRemainingInString();
            });

            if (IsDone()) MakeStop();
        }

        private string GetRemainingInString()
        {
            return $"{Remaining.Hours:00}:{Remaining.Minutes:00}";
        }

        private DateTime GetTotalTimeFromSettings(string defaultTotalTime)
        {
            var tt = TimeSpan.Parse(defaultTotalTime, CultureInfo.InvariantCulture);
            return DateTime.Now + tt;
        }

        private bool IsDone()
        {
            return DateTime.Now >= _endTime;
        }

        private void MakeStop()
        {
            _aTimer.Stop();

            const string CAPTION = "vWorkus";

            string messageBoxText = $"Relax now, job is done. \n{StartTime:HH.mm}-{DateTime.Now:HH:mm}";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;

            PlayAudio(Settings.Default.AlertSoundPath);

            Dispatcher.Invoke(() =>
            {
                LbCountdown.Foreground = Brushes.Red;
            });

            MessageBox.Show(messageBoxText, CAPTION, button, icon, MessageBoxResult.Yes);
            
            Environment.Exit(0);
        }

        public static void PlayAudio(string fileName)
        {
            using var audioFile = new AudioFileReader(fileName);
            using var outputDevice = new WaveOutEvent();

            outputDevice.Init(audioFile);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
