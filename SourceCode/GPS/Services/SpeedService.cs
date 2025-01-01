using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Services
{
    public partial class SpeedService
    {
        private readonly Timer _timer;
        private readonly TSConnectionService _tsConnectionService;
        private readonly FormGPS _formGPS;
        private readonly TSDataSender _dataSenderTS;

        public SpeedService(FormGPS formGPS)
        {
            _formGPS = formGPS;
            _tsConnectionService = TSConnectionService.Instance;
            _dataSenderTS = TSDataSender.Instance;
            _timer = new Timer(async _ => await SendSpeedData(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _formGPS = formGPS;
        }

        private async Task SendSpeedData()
        {
            // Sprawdzamy, czy połączenie jest aktywne
            if (_tsConnectionService.IsConnectedToTS)
            {
                if (double.TryParse(_formGPS.SpeedKPH, out double currentSpeed))
                {
                    _dataSenderTS.SendData(new
                    {
                        speed = currentSpeed / 3.6
                    });
                }
                await Task.CompletedTask;
            }
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }
    }

    #region Class structure
    public partial class SpeedService
    {
        private static Lazy<SpeedService> _lazyInstance = null;

        public static SpeedService Initialize(FormGPS formGps)
        {
            if (_lazyInstance == null)
            {
                _lazyInstance = new Lazy<SpeedService>(() => new SpeedService(formGps));
            }

            return _lazyInstance.Value;
        }

        public static SpeedService Instance
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
