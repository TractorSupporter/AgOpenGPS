using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace AgOpenGPS.Services
{
    public partial class TSDataReceiver
    {
        private readonly string _pipeName = "ts_pipe_to_gps";
        private readonly NamedPipeServerStream _pipeServer;
        private readonly StreamReader _reader;
        public event Action<double> DistanceReceived;
        CultureInfo culture = new CultureInfo("fr-FR");

        private TSDataReceiver()
        {
            _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In);
            _reader = new StreamReader(_pipeServer);
        }

        public async Task StartReceivingAsync()
        {
            _pipeServer.WaitForConnection();
            while (_pipeServer.IsConnected)
            {
                string message = await _reader.ReadLineAsync();

                if (double.TryParse(message, NumberStyles.Any, culture, out double distance))
                {
                    DistanceReceived?.Invoke(distance);
                }
                else
                {
                    Console.WriteLine("Failed to parse the input string.");
                }
            }
        }
    }

    public partial class TSDataReceiver : IDisposable
    {
        private static readonly Lazy<TSDataReceiver> _lazyInstance = new Lazy<TSDataReceiver>(() => new TSDataReceiver());
        public static TSDataReceiver Instance => _lazyInstance.Value;

        public void Dispose()
        {
            _reader?.Dispose();
            _pipeServer?.Dispose();
        }
    }
}
