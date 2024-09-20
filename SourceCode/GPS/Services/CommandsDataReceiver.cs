using System;
using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;

namespace AgOpenGPS.Services
{
    public class CommandsDataReceiver : IDisposable
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly StreamReader _reader;
        public event Action AvoidCommandReceived;
        public event Action AlarmCommandReceived;
        CultureInfo culture = new CultureInfo("fr-FR");

        public CommandsDataReceiver(string pipeName)
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

                if (message == "alarm")
                {
                    AlarmCommandReceived?.Invoke();
                }
                else if (message == "avoid")
                {
                    AvoidCommandReceived?.Invoke();
                }
                else
                {
                    Console.WriteLine("Failed to recogize the input string.");
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
