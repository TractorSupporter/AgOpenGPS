﻿using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json.Linq;
using AgOpenGPS.Types;

namespace AgOpenGPS.Services
{
    public partial class TSDataReceiver
    {
        public event Action<TurnType> ReceivedAvoidingDecision;
        public event Action<double> ReceivedDistanceMeasured;
        public event Action<double> ReceivedAlarmDecision;
        public event Action ReceivedApplicationStateQuery;
        private readonly TSConnectionService _tsConnectionService;
        private TSDataSender _dataSenderTS;

        private TSDataReceiver()
        {
            _dataSenderTS = TSDataSender.Instance;
            _tsConnectionService = TSConnectionService.Instance;
            ReceivedApplicationStateQuery += RespondForApplicationStateQuery;
        }

        public void RespondForApplicationStateQuery()
        {
            _ = _dataSenderTS.SendData(new
            {
                allowAlarmDecision = AlarmService._isAlarmAllowed,
                allowAvoidingDecision = AvoidingService._isAvoidingAllowed,
                vehicleWidth = AvoidingService.Instance.VehicleWidth
            });
        }


        public async Task StartReceivingAsync()
        {
            while (true)
            {
                while (_tsConnectionService.IsConnectedToTS)
                {
                    try
                    {
                        JObject data = JObject.Parse(await _tsConnectionService.ReadFromPipe());

                        if (data.TryGetValue("shouldAvoid", out JToken shouldAvoidToken))
                        {
                            bool turnDirection = data.GetValue("turnDirection").Value<bool>();

                            TurnType type = turnDirection ? TurnType.Right : TurnType.Left;

                            bool shouldAvoid = shouldAvoidToken.Value<bool>();
                            if (shouldAvoid)
                                ReceivedAvoidingDecision.Invoke(type);
                        }
                        if (data.TryGetValue("distanceMeasured", out JToken distanceMeasuredToken))
                        {
                            ReceivedDistanceMeasured.Invoke(distanceMeasuredToken.Value<double>());
                        }
                        if (data.TryGetValue("shouldAlarm", out JToken shouldAlarmToken))
                        {
                            bool decision = shouldAlarmToken.Value<bool>();

                            if (decision)
                            {
                                data.TryGetValue("angle", out JToken angleToken);


                                ReceivedAlarmDecision.Invoke(angleToken.Value<double>());
                            }
                        }
                        if (data.TryGetValue("askForApplicationState", out JToken avoidingAllowedQuery))
                        {
                            ReceivedApplicationStateQuery.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        await _tsConnectionService.StayConnectedToTSAsync();
                    }
                }
                

                await _tsConnectionService.StayConnectedToTSAsync();
            }
        }


        //    private readonly string _pipeName = "ts_pipe_to_gps";
        //    private readonly NamedPipeServerStream _pipeServer;
        //    private readonly StreamReader _reader;
        //    public event Action<double> DistanceReceived;
        //    public event Action AvoidingDecisionMade;
        //    public event Action AlarmCommandReceived;
        //    public event Action AlarmCommandNotReceived;
        //    CultureInfo culture = new CultureInfo("fr-FR");

        //    private TSDataReceiver()
        //    {
        //        _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In);
        //        _reader = new StreamReader(_pipeServer);
        //    }

        //    public async Task StartReceivingAsync()
        //    {
        //        _pipeServer.WaitForConnection();
        //        while (_pipeServer.IsConnected)
        //        {
        //            JObject data = JObject.Parse(await _reader.ReadLineAsync());

        //            if (data.TryGetValue("shouldAvoid", out JToken shouldAvoidToken))
        //            {
        //                bool shouldAvoid = shouldAvoidToken.Value<bool>();
        //                if (shouldAvoid)
        //                    AvoidingDecisionMade?.Invoke();
        //            }
        //            if (data.TryGetValue("distanceMeasured", out JToken distanceMeasuredToken))
        //            {
        //                DistanceReceived?.Invoke(distanceMeasuredToken.Value<double>());
        //            }
        //            if (data.TryGetValue("shouldAlarm", out JToken shouldAlarmToken))
        //            {
        //                bool shouldAlarm = shouldAlarmToken.Value<bool>();
        //                if (shouldAlarm)
        //                    AlarmCommandReceived?.Invoke();
        //                else AlarmCommandNotReceived?.Invoke();
        //            }
        //            else
        //            {
        //                Console.WriteLine("Failed to parse the input elements.");
        //            }
        //        }
        //    }
    }

    public partial class TSDataReceiver
    {
        private static readonly Lazy<TSDataReceiver> _lazyInstance = new Lazy<TSDataReceiver>(() => new TSDataReceiver());
        public static TSDataReceiver Instance => _lazyInstance.Value;
    }
}
