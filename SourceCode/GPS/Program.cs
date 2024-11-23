using AgOpenGPS.Properties;
using AgOpenGPS.Services;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgOpenGPS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static readonly Mutex Mutex = new Mutex(true, "{516-0AC5-B9A1-55fd-A8CE-72F04E6BDE8F}");
        private static FormGPS formGPS;
        private static TSDataSender dataSenderTS;
        private static AlarmService alarmService;
        private static AvoidingService avoidingService;
        private static double distanceToObstacle;

        [STAThread]
        private static void Main()
        {
            ////opening the subkey
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AgOpenGPS");

            ////create default keys if not existing
            if (regKey == null)
            {
                RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");

                //storing the values
                Key.SetValue("Language", "en");
                Key.Close();

                Settings.Default.setF_culture = "en";
                Settings.Default.Save();
            }
            else
            {
                //check for corrupt settings file
                try
                {
                    Settings.Default.setF_culture = regKey.GetValue("Language").ToString();
                }
                catch (System.Configuration.ConfigurationErrorsException ex)
                {
                    // Corrupted XML! Delete the file, the user can just reload when this fails to appear. No need to worry them
                    MessageBoxButtons btns = MessageBoxButtons.OK;
                    System.Windows.Forms.MessageBox.Show("Error detected in config file - fixing it now, please close this and restart app", "Problem!", btns);
                    string filename = ((ex.InnerException as System.Configuration.ConfigurationErrorsException)?.Filename) as string;
                    System.IO.File.Delete(filename);
                    Settings.Default.Reload();
                    Application.Exit();
                }

                Settings.Default.Save();
                regKey.Close();
            }

            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Properties.Settings.Default.setF_culture);
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Properties.Settings.Default.setF_culture);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                dataSenderTS = TSDataSender.Instance;
                formGPS = new FormGPS();
                avoidingService = AvoidingService.Instance;
                alarmService = AlarmService.Instance;

                StartTSDataReceiverAsync();
                //StartCommandsDataReceiverAsync();

                // Run the main form
                Application.Run(formGPS);
            }
            else
            {
                MessageBox.Show("AgOpenGPS is Already Running");
            }
        }

        private static async void StartTSDataReceiverAsync()
        {
            var receiver = TSDataReceiver.Instance;
            receiver.ReceivedDistanceMeasured += OnDistanceReceived;
            receiver.ReceivedAlarmDecision += OnAlarmCommandReceived;
            receiver.ReceivedAlarmDecision += OnAlarmCommandNotReceived;
            await receiver.StartReceivingAsync();
        }

        private static void OnAlarmCommandReceived(bool shouldAlarm)
        {
            if (shouldAlarm)
                alarmService.PlayAlarm(distanceToObstacle);
        }

        private static void OnAlarmCommandNotReceived(bool shouldAlarm)
        {
            if (!shouldAlarm)
                alarmService.StopAlarm();
        }

        //private static void OnAvoidCommandReceived()
        //{
        //    // Process the distance data as needed
        //    Console.WriteLine($"Received command: avoid");

        //}

        private static void OnDistanceReceived(double distance)
        {
            distanceToObstacle = distance;
            Console.WriteLine($"distance: {distance}");
        }

        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //private static extern bool SetProcessDPIAware();
    }
}
