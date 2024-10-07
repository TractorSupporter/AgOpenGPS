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
        private static TSCommandsDataSender commandsTSDataSender;

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

                commandsTSDataSender = new TSCommandsDataSender("UnblockCommandsPipe");
                formGPS = new FormGPS();
                formGPS.TSCommandStateChange += OnTSCommandStateChange;

                StartDistanceDataReceiverAsync();
                StartCommandsDataReceiverAsync();

                // Run the main form
                Application.Run(formGPS);
            }
            else
            {
                MessageBox.Show("AgOpenGPS is Already Running");
            }
        }

        private static void OnTSCommandStateChange(string data)
        {
            commandsTSDataSender.SendCommandData(data);
        }

        private static async void StartCommandsDataReceiverAsync()
        {
            using (var receiver = new CommandsDataReceiver("CommandsPipe"))
            {
                receiver.AvoidCommandReceived += OnAvoidCommandReceived;
                receiver.AlarmCommandReceived += OnAlarmCommandReceived;

                await receiver.StartReceivingAsync();
            }
        }

        private static async void StartDistanceDataReceiverAsync()
        {
            using (var receiver = new DistanceDataReceiver("DistancePipe"))
            {
                receiver.DistanceReceived += OnDistanceReceived;
                await receiver.StartReceivingAsync();
            }
        }

        private static void OnAlarmCommandReceived()
        {
            // Process the distance data as needed
            Console.WriteLine($"Received command: alarm");
        }
        
        private static void OnAvoidCommandReceived()
        {
            // Process the distance data as needed
            Console.WriteLine($"Received command: avoid");
            if (formGPS.isLateralOn)
            {
                formGPS.yt.BuildManualYouLateral(true);
                formGPS.yt.ResetYouTurn();

                Task.Run(() => DelayAndUnlockAvoidCommand());
            }
        }

        private static async Task DelayAndUnlockAvoidCommand()
        {
            await Task.Delay(AvoidCommandDelayTime);

            commandsTSDataSender.SendCommandData("unblock_avoid");
        }

        private static void OnDistanceReceived(double distance)
        {
            // Process the distance data as needed
            Console.WriteLine($"Received distance: {distance}");
        }

        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //private static extern bool SetProcessDPIAware();
    }
}