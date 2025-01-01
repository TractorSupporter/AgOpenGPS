using System;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AgOpenGPS.Services
{
    public partial class AlarmService
    {
        public static bool _isAlarmAllowed = false;
        private Timer _alarmTimer;
        private static FormGPS _formGPS;
        public bool IsAlarmPlaying {get; set;}
        public bool IsRed { get; set; }
        public PlaceFlagService _placeFlagService;
        private readonly TSDataSender _dataSenderTS;
        private readonly TSConnectionService _tsConnectionService;
        private double distanceToObstacle;

        private AlarmService(FormGPS formGPS)
        {
            IsRed = true;
            _placeFlagService = PlaceFlagService.Instance;
            _formGPS = formGPS;
            _alarmTimer = new Timer();
            _alarmTimer.Interval = 1000;
            _alarmTimer.Tick += AlarmTimer_Tick;
            _dataSenderTS = TSDataSender.Instance;
            TSDataReceiver.Instance.ReceivedAlarmDecision += MakeAlarmDecision;
            _tsConnectionService = TSConnectionService.Instance;
        }

        public void SetDistanceToObstacle(double distance)
        {
            distanceToObstacle = distance;
        }

        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            if (_tsConnectionService.IsConnectedToTS)
            {
                _formGPS.sounds.obstacleAlarm.Play();
                IsRed = !IsRed;
            } else
            {
                StopAlarm();
            }
        }

        public void MakeAlarmDecision(double angle)
        {
            _isAlarmAllowed = false;
            PlayAlarm(distanceToObstacle, angle);

            _ = CancelAlarm();
        }

        public async Task CancelAlarm()
        {
            
            var time = distanceToObstacle / Math.Max(_formGPS.avgSpeed, 0.01) * 36 * 1.2 ; // 36 for converting kmh to cm/ms and avoiding slows down vehicle a bit

            await Task.Delay((int)time + 1);

            StopAlarm();
            AllowTSAlarmCommand();
        }

        public void AllowTSAlarmCommand()
        {
            if (_isAlarmAllowed) return;
            _ = _dataSenderTS.SendData(new
            {
                allowAlarmDecision = true
            });
            _isAlarmAllowed = true;
        }

        public void ForbidTSAlarmCommand()
        {
            if (!_isAlarmAllowed) return;
            _ = _dataSenderTS.SendData(new
            {
                allowAlarmDecision = false
            });
            _isAlarmAllowed = false;
        }










        

        public void PlayAlarm(double distance, double objectDirectionFromHeadingInDegrees = 0)
        {
            if (_formGPS.isBtnAutoSteerOn)
            {
                IsAlarmPlaying = true;
                if (!_alarmTimer.Enabled)
                {
                    var heading = _formGPS.fixHeading + objectDirectionFromHeadingInDegrees * Math.PI / 180.0;

                    double distanceInMeters = distance / 100 + 1;
                    if (_formGPS.vehicle.vehicleType != 1)
                    {
                        distanceInMeters += _formGPS.vehicle.wheelbase;
                    }
                    double newEasting = _formGPS.pn.fix.easting + distanceInMeters * Math.Sin(heading);
                    double newNorthing = _formGPS.pn.fix.northing + distanceInMeters * Math.Cos(heading);


                    _placeFlagService.placeFlag(_formGPS, _formGPS.flagPts, _formGPS.pn, _formGPS.fixHeading, _formGPS.flagColor, newEasting, newNorthing, false);
                    _alarmTimer.Start();
                }
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
