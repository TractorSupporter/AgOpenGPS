using System;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace AgOpenGPS.Services
{
    public partial class AlarmService
    {
        private Timer _alarmTimer;
        private static FormGPS _formGPS;
        public bool IsAlarmPlaying {get; set;}
        public bool IsRed { get; set; }
        public PlaceFlagService _placeFlagService;

        private AlarmService(FormGPS formGPS)
        {
            IsRed = true;
            _placeFlagService = PlaceFlagService.Instance;
            _formGPS = formGPS;
            _alarmTimer = new Timer();
            _alarmTimer.Interval = 1000;
            _alarmTimer.Tick += AlarmTimer_Tick;
            
        }

        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            _formGPS.sounds.obstacleAlarm.Play();
            IsRed = !IsRed;
        }

        public void PlayAlarm(double distance)
        {
            IsAlarmPlaying = true;
            if (!_alarmTimer.Enabled)
            {
                double distanceInMeters = distance / 100;
                double newEasting = _formGPS.pn.fix.easting + distanceInMeters * _formGPS.sinSectionHeading;
                double newNorthing = _formGPS.pn.fix.northing + distanceInMeters * _formGPS.cosSectionHeading;

                _placeFlagService.placeFlag(_formGPS, _formGPS.flagPts, _formGPS.pn, _formGPS.fixHeading, _formGPS.flagColor, newEasting, newNorthing);
                _alarmTimer.Start();
            }
        }

        public void StopAlarm()
        {
            IsAlarmPlaying = false;
            if (_alarmTimer.Enabled)
            {
                _alarmTimer.Stop();
            }
        }

        public Color getAlertColor()
        {
            return IsRed ? Color.FromArgb(255, 255, 0, 0) : Color.FromArgb(255, 255, 128, 0);
        }
    }

    #region Class structure
    public partial class AlarmService
    {
        private static Lazy<AlarmService> _lazyInstance = null;

        public static AlarmService Initialize(FormGPS formGps)
        {
            if (_lazyInstance == null)
            {
                _lazyInstance = new Lazy<AlarmService>(() => new AlarmService(formGps));
            }

            return _lazyInstance.Value;
        }

        public static AlarmService Instance
        {
            get
            {
                if (_lazyInstance == null)
                {
                    throw new Exception("AlarmService not initialized");
                }

                return _lazyInstance.Value;
            }
        }
    }
    #endregion
}
