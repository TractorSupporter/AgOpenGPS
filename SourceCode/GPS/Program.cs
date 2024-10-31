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
        private static int AvoidCommandDelayTime = 4000;
        private static TSDataSender dataSenderTS;
        private static AlarmService alarmService;
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
                formGPS.AvoidingCommandStateChangeTS += OnAvoidingCommandStateChangeTS;

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



        private static async void OnAvoidingCommandStateChangeTS(bool shouldAllowAvoidingDecision)
        {
            if (FormGPS._isAvoidingAllowed == shouldAllowAvoidingDecision) return;

            await dataSenderTS.SendDataAsync(new
            {
                allowAvoidingDecision = shouldAllowAvoidingDecision
            });

            FormGPS._isAvoidingAllowed = shouldAllowAvoidingDecision;
        }

        //private static async void StartCommandsDataReceiverAsync()
        //{
        //    using (var receiver = new CommandsDataReceiver("CommandsPipe"))
        //    {
        //        receiver.AvoidCommandReceived += OnAvoidCommandReceived;
        //        receiver.AlarmCommandReceived += OnAlarmCommandReceived;

        //        await receiver.StartReceivingAsync();
        //    }
        //}

        private static void StartTSDataReceiverAsync()
        {
            var receiver = TSDataReceiver.Instance;
            receiver.DistanceReceived += OnDistanceReceived;
            receiver.AvoidingDecisionMade += OnAvoidingDecisionMade;
            receiver.AlarmCommandReceived += OnAlarmCommandReceived;
            receiver.AlarmCommandNotReceived += OnAlarmCommandNotReceived;
            _ = receiver.StartReceivingAsync();
        }

        private static void OnAvoidingDecisionMade()
        {
            if (formGPS.isLateralOn)
            {
                formGPS.yt.BuildManualYouLateral(true);
                formGPS.yt.ResetYouTurn();
            }
        }

        private static void OnAlarmCommandReceived()
        {
            alarmService.PlayAlarm(distanceToObstacle);
        }

        private static void OnAlarmCommandNotReceived()
        {
            alarmService.StopAlarm();
        }

        //private static void OnAvoidCommandReceived()
        //{
        //    // Process the distance data as needed
        //    Console.WriteLine($"Received command: avoid");

        //}

        private static async Task DelayAndUnlockAvoidCommand()
        { // TODO change this delay to sth that makes more sense
            await Task.Delay(AvoidCommandDelayTime);

            await dataSenderTS.SendDataAsync(new
            {
                allowAvoidingDecision = true
            });
        }

        private static void OnDistanceReceived(double distance)
        {
            distanceToObstacle = distance;
        }

        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //private static extern bool SetProcessDPIAware();
    }
}
