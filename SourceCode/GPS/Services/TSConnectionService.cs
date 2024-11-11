using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Services
{
    public partial class TSConnectionService
    {
        private readonly string _pipeName;
        private NamedPipeServerStream _pipeServer;
        private StreamReader _reader;
        private StreamWriter _writer;
        private readonly SemaphoreSlim _connectToTSSemaphore;
        private CancellationTokenSource _cancellationTokenSource;
        public bool IsConnecting { get; private set; }
        public bool IsConnectedToTS => _pipeServer.IsConnected;

        private TSConnectionService()
        {
            _pipeName = "ts_gps_pipe";
            _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _reader = new StreamReader(_pipeServer);
            _cancellationTokenSource = new CancellationTokenSource();
            _connectToTSSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task StayConnectedToTSAsync()
        {
            if (IsConnectedToTS)
                return;

            await _connectToTSSemaphore.WaitAsync();

            if (IsConnectedToTS)
                return;

            _pipeServer.Dispose();
            _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _reader = new StreamReader(_pipeServer);

            IsConnecting = true;

            await _pipeServer.WaitForConnectionAsync();
            _writer = new StreamWriter(_pipeServer) { AutoFlush = true };

            IsConnecting = false;
            _connectToTSSemaphore.Release();
        }

        public void WriteToPipe(string data)
        {
            _writer.WriteLine(data);
        }

        public async Task<string> ReadFromPipe()
        {
            return await _reader.ReadLineAsync() ?? "";
        }


        //private readonly string _inputPipeName;
        //private readonly NamedPipeServerStream _inputPipeServer;
        //private readonly StreamReader _inputReader;
        //private readonly string _outputPipeName;
        //private readonly NamedPipeClientStream _outputPipeClient;
        //private StreamWriter _outputWriter;
        //private readonly int _outputMaxRetryAttempts;
        //private readonly int _outputRetryDelayMilliseconds;
        //private readonly SemaphoreSlim _connectToTSSemaphore;
        //private CancellationTokenSource _cancellationTokenSource;

        //public bool IsConnecting { get; private set; }
        //public bool IsConnectedToTS => _outputPipeClient.IsConnected && _inputPipeServer.IsConnected;

        //public async Task Connect()
        //{
        //    _cancellationTokenSource = new CancellationTokenSource();
        //    await StayConnectedToTSAsync(_cancellationTokenSource.Token);
        //}

        //private TSConnectionService()
        //{
        //    _inputPipeName = "ts_pipe_to_gps";
        //    _inputPipeServer = new NamedPipeServerStream(_inputPipeName, PipeDirection.In);
        //    _inputReader = new StreamReader(_inputPipeServer);

        //    _outputPipeName = "ts_pipe_from_gps";
        //    _outputPipeClient = new NamedPipeClientStream(".", _outputPipeName, PipeDirection.Out);

        //    _cancellationTokenSource = new CancellationTokenSource();
        //    _connectToTSSemaphore = new SemaphoreSlim(1, 1);
        //}

        //private async Task StayConnectedToTSAsync(CancellationToken token)
        //{
        //    if (IsConnectedToTS)
        //        return;

        //    await _connectToTSSemaphore.WaitAsync();
        //    try
        //    {
        //        if (IsConnectedToTS)
        //            return;

        //        IsConnecting = true;

        //        if (_outputPipeClient.IsConnected)
        //            _outputPipeClient.Close();
        //        if (_inputPipeServer.IsConnected)
        //            _inputPipeServer.Disconnect();

        //        await Task.WhenAll(_outputPipeClient.ConnectAsync(token), _inputPipeServer.WaitForConnectionAsync(token));
        //        _outputWriter = new StreamWriter(_outputPipeClient) { AutoFlush = true };
        //    }
        //    catch (AggregateException e)
        //    {
        //        Exception inner = e.InnerException;
        //        if (!(inner is TaskCanceledException))
        //            throw inner;
        //    }
        //    finally
        //    {
        //        IsConnecting = false;
        //        _connectToTSSemaphore.Release();
        //    }
        //}

        //public void WriteToOutputPipe(string data)
        //{
        //    _outputWriter.WriteLine(data);
        //}

        //public async Task<string> ReadFromInputPipe()
        //{
        //    return await _inputReader.ReadLineAsync() ?? "";
        //}
    }

    #region Class structure
    public partial class TSConnectionService
    {
        private static readonly Lazy<TSConnectionService> _lazyInstance = new Lazy<TSConnectionService>(() => new TSConnectionService());
        public static TSConnectionService Instance => _lazyInstance.Value;
    }
    #endregion
}
