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
    public class DistanceDataReceiver : IDisposable
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly StreamReader _reader;
        public event Action<double> DistanceReceived;
        CultureInfo culture = new CultureInfo("fr-FR");

        public DistanceDataReceiver(string pipeName)
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
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

        public void Dispose()
        {
            _reader?.Dispose();
            _pipeServer?.Dispose();
        }
    }
}
