using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgOpenGPS.Services
{
    public partial class AlarmService
    {
        private Timer _alarmTimer;
        private static FormGPS _formGPS;
        public static bool isAlarmPlaying {get; set;}
        public static bool isRed { get; set; }

        private AlarmService(FormGPS formGPS)
        {
            isRed = true;
            _formGPS = formGPS;
            _alarmTimer = new Timer();
            _alarmTimer.Interval = 1000;
            _alarmTimer.Tick += AlarmTimer_Tick;
        }

        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            _formGPS.sounds.obstacleAlarm.Play();
            isRed = !isRed;
        }

        public void PlayAlarm()
        {
            isAlarmPlaying = true;
            if (!_alarmTimer.Enabled)
            {
                _alarmTimer.Start();
            }
        }

        public void StopAlarm()
        {
            isAlarmPlaying = false;
            if (_alarmTimer.Enabled)
            {
                _alarmTimer.Stop();
            }
        }
    }

    public partial class AlarmService
    {
        private static AlarmService _instance;

        public static AlarmService Instance(FormGPS formGPS)
        {
            if (_instance == null)
            {
                _instance = new AlarmService(formGPS);
            }
            return _instance;
        }
    }
}
