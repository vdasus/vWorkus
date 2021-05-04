using System;
using System.Globalization;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NAudio.Wave;
using NLog;
using vWorkus.Properties;
using Timer = System.Timers.Timer;

namespace vWorkus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static int TimeDelta => Settings.Default.TimeDelta;

        private static Timer _aTimer;
        private DateTime _endTime;
        private int _step;
        private string _pathToAlertFile;

        private TimeSpan Remaining => _endTime - DateTime.Now;

        private DateTime StartTime { get; set; }


        public MainWindow()
        {
            StartTime = DateTime.Now;
            Log.Info($"Started at {StartTime:HH:mm}");
            
            InitializeComponent();

            PrepareSettings();
            SetTimer();
            ShowInTaskbar = false;
        }

        private void PrepareSettings()
        {
            const int MINUTE_TO_MS = 1000 * 60;

            _endTime = GetTotalTimeFromSettings(Settings.Default.TotalTime);
            Log.Trace($"Settings ET: {Settings.Default.TotalTime}");
            Log.Trace($"Prepared ET: {_endTime}");

            _step = Settings.Default.StepInMinutes * MINUTE_TO_MS;
            _pathToAlertFile = Settings.Default.AlertSoundPath;

            Dispatcher.Invoke(() =>
            {
                BtPlusTime.Content = "+" + TimeDelta;
                BtMinusTime.Content = "-" + TimeDelta;
            });

            ResetCaption();
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
            ResetCaption();
            if (IsDone()) MakeStop();
        }

        private string GetRemainingInString()
        {
            var sep = _aTimer is {Enabled: false} ? " || " : "";
            return $@"{sep}{Remaining.Hours:00}:{Remaining.Minutes:00}{sep}";
        }

        private DateTime GetTotalTimeFromSettings(string defaultTotalTime)
        {
            var tt = TimeSpan.Parse(defaultTotalTime, CultureInfo.InvariantCulture);
            return DateTime.Now + tt;
        }

        private void ResetCaption()
        {
            Dispatcher.Invoke(() =>
            {
                LbCountdown.Content = GetRemainingInString();
            });
        }

        private bool IsDone()
        {
            return DateTime.Now > _endTime;
        }

        private void MakeStop()
        {
            const string CAPTION = "vWorkus";

            _aTimer.Stop();

            Log.Info($"Stopped at {DateTime.Now:HH:mm}");

            string messageBoxText = $"Relax now, job is done. \n{StartTime:HH.mm}-{DateTime.Now:HH:mm}";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;

            PlayAudio(_pathToAlertFile);

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

        private void ToggleVisibility_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Log.Info($"Stopped by exit at {DateTime.Now:HH:mm}");
            Environment.Exit(0);
        }

        private void BtSwitchPause_Click(object sender, RoutedEventArgs e)
        {
            Log.Info(@$"Start\pause at {DateTime.Now:HH:mm}");

            _aTimer.Enabled = !_aTimer.Enabled;
            BtSwitchPause.Content = _aTimer.Enabled ? "Pause" : "Start";
            ResetCaption();
        }

        private void BtPlus_OnClick(object sender, RoutedEventArgs e)
        {
            _endTime += TimeSpan.FromMinutes(TimeDelta);
            ResetCaption();
        }

        private void BtMinus_OnClick(object sender, RoutedEventArgs e)
        {
            _endTime -= TimeSpan.FromMinutes(TimeDelta);
            ResetCaption();
        }

        private void BtExit_OnClick(object sender, RoutedEventArgs e)
        {
            Exit_OnClick(sender, e);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
