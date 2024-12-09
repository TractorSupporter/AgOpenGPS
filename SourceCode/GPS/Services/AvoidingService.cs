using AgOpenGPS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgOpenGPS.Services
{
    public partial class AvoidingService
    {
        public static bool _isAvoidingAllowed = false;
        private readonly TSDataSender _dataSenderTS;
        private readonly double _minAllowAvoidCommandDistance;

        private readonly FormGPS _formGPS;
        private AvoidingService(FormGPS formGPS)
        {
            _minAllowAvoidCommandDistance = 20;
            _dataSenderTS = TSDataSender.Instance;
            _formGPS = formGPS;
            TSDataReceiver.Instance.ReceivedAvoidingDecision += Avoid;
            TSDataReceiver.Instance.ReceivedAvoidingAllowedQuery += RespondForAvoidingAllowedQuery;
        }

        public void RespondForAvoidingAllowedQuery()
        {
            _ = _dataSenderTS.SendData(new
            {
                allowAvoidingDecision = _isAvoidingAllowed
            });
        }

        public void DisallowAvoiding() => _isAvoidingAllowed = false;

        public void Avoid(TurnType type)
        {
            if (_formGPS.isLateralOn)
            {
                _formGPS.yt.BuildManualYouLateral(TurnType.Left != type);
                _formGPS.yt.ResetYouTurn();
            }
        }

        public bool ShouldAllowAvoidingCommand(double avgPivotDistance, bool isBtnAutoSteerOn)
        {
            return Math.Abs(avgPivotDistance) <= _minAllowAvoidCommandDistance && !_isAvoidingAllowed && isBtnAutoSteerOn;
        }

        public void AllowTSAvoidingCommand()
        {
            if (_isAvoidingAllowed == true) return;
            _ = _dataSenderTS.SendData(new
            {
                allowAvoidingDecision = true
            });
            _isAvoidingAllowed = true;
        }

        public void ForbidTSAvoidingCommand()
        {
            if (_isAvoidingAllowed == false) return;
            _ = _dataSenderTS.SendData(new
            {
                allowAvoidingDecision = false
            });
            _isAvoidingAllowed = false;
        }
    }

    #region Class structure
    public partial class AvoidingService
    {
        private static Lazy<AvoidingService> _lazyInstance = null;

        public static AvoidingService Initialize(FormGPS formGps)
        {
            if (_lazyInstance == null)
            {
                _lazyInstance = new Lazy<AvoidingService>(() => new AvoidingService(formGps));
            }

            return _lazyInstance.Value;
        }

        public static AvoidingService Instance
        {
            get
            {
                if (_lazyInstance == null)
                {
                    throw new Exception("AvoidingService not initialized");
                }

                return _lazyInstance.Value;
            }
        }
    }
    #endregion
}
