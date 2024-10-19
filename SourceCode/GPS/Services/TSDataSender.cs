using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AgOpenGPS.Services
{
    public partial class TSDataSender
    {
        private string message = string.Empty;
        private readonly string _pipeName = "ts_pipe_from_gps";
        private NamedPipeClientStream _pipeClient;
        private StreamWriter _writer;
        private const int MaxRetryAttempts = 8;
        private const int RetryDelayMilliseconds = 1000;

        private TSDataSender()
        {
            ConnectToPipe();
        }

        private void ConnectToPipe()
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            _pipeClient.Connect();
            _writer = new StreamWriter(_pipeClient) { AutoFlush = true };
        }

        public void SendData(object data)
        {
            message = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            if (_pipeClient.IsConnected)
            {
                try
                {
                    _writer.WriteLine(message);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Pipe is broken: " + ex.Message);
                    RetrySendData();
                }
            }
            else
            {
                Console.WriteLine("Pipe is not connected.");
                RetrySendData();
            }
        }

        private void RetrySendData()
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"Attempting to reconnect... (Attempt {attempt})");
                    ConnectToPipe();
                    _writer.WriteLine(message);
                    Console.WriteLine("Reconnected and sent data successfully.");
                    return;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Reconnection attempt {attempt} failed: {ex.Message}");
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }

            Console.WriteLine("Failed to reconnect after multiple attempts.");
        }
    }

    #region Class structure
    public partial class TSDataSender: IDisposable
    {
        public static TSDataSender Instance => _lazyInstance.Value;
        private static readonly Lazy<TSDataSender> _lazyInstance = new Lazy<TSDataSender>(() => new TSDataSender());

        public void Dispose()
        {
            _writer?.Dispose();
            _pipeClient?.Dispose();
        }
    }
    #endregion
}
