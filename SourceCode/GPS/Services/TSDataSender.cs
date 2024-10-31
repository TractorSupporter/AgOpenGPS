using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace AgOpenGPS.Services
{
    public partial class TSDataSender
    {
        private readonly string _pipeName = "ts_pipe_from_gps";
        private NamedPipeClientStream _pipeClient;
        private StreamWriter _writer;
        private const int MaxRetryAttempts = 8;
        private const int RetryDelayMilliseconds = 1000;

        private TSDataSender() {}

        private async Task ConnectToPipeAsync()
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await _pipeClient.ConnectAsync();
            _writer = new StreamWriter(_pipeClient) { AutoFlush = true };
        }

        public async Task SendDataAsync(object data)
        {
            string message = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                try
                {
                    await ConnectToPipeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to connect to pipe: " + ex.Message);
                    // Optionally handle connection failure here
                    return;
                }
            }

            try
            {
                await _writer.WriteLineAsync(message);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Pipe is broken: " + ex.Message);
                await RetrySendDataAsync(message);
            }
        }

        private async Task RetrySendDataAsync(string message)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"Attempting to reconnect... (Attempt {attempt})");
                    await ConnectToPipeAsync();
                    await _writer.WriteLineAsync(message);
                    Console.WriteLine("Reconnected and sent data successfully.");
                    return;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Reconnection attempt {attempt} failed: {ex.Message}");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }

            Console.WriteLine("Failed to reconnect after multiple attempts.");
        }
    }

    #region Class structure
    public partial class TSDataSender : IDisposable
    {
        private static readonly Lazy<TSDataSender> _lazyInstance = new Lazy<TSDataSender>(() => new TSDataSender());
        public static TSDataSender Instance => _lazyInstance.Value;

        public void Dispose()
        {
            _writer?.Dispose();
            _pipeClient?.Dispose();
        }
    }
    #endregion
}
