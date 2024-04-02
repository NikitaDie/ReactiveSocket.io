using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MessengerProtocolRealization.Message;
using ReactiveSocketIO.Core;
using ReactiveSocketIO.Core.Message;

namespace ReactiveSocketIO.BaseImplementation.Transport;

public class TcpTransport : ITransport
{
    // ReSharper disable once InconsistentNaming
    private const int PACKET_COUNT_OF_LENGHT = 4;
    private readonly TcpClient _client;
    private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
    private NetworkStream? _netStream;
    private readonly IPEndPoint _remoteEndPoint;
    private Task? _receiveTask;
    public bool IsInitialized { get; private set; }
    public IMessageBuilder MessageBuilder { get; init; } = new MessageBuilder();
    
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<IMessage>? OnReceived;
    public event Action<Exception>? OnError;
    
    public TcpTransport(TcpClient client, string connString = "") 
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        
        if (!string.IsNullOrEmpty(connString))
            _remoteEndPoint = GetIPEndPoint(connString);
        else
            _remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint ?? throw new InvalidOperationException(); //TODO: Exception
    }
    
    public TcpTransport(string connString)
    {
        _client = new TcpClient();
        _remoteEndPoint = GetIPEndPoint(connString);
    }
    
    private static IPEndPoint GetIPEndPoint(string connString)
    {
        return IPEndPoint.Parse(connString);
    }
    
    public void Initialize()
    {
        try
        {
            if (!_client.Connected)
                _client.Connect(_remoteEndPoint);
            
            _netStream = _client.GetStream();
            _receiveTask = Task.Run(ReceiveLoop, _cancellationSource.Token);
            IsInitialized = true;
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            throw;
        }
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            if (!_client.Connected)
                await _client.ConnectAsync(_remoteEndPoint);
            
            _netStream = _client.GetStream();
            _receiveTask = Task.Run(ReceiveLoop, _cancellationSource.Token);
            IsInitialized = true;
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            throw;
        }
    }
 
    private void ReceiveLoop()
    {
        try
        {
            while (!_cancellationSource.Token.IsCancellationRequested)
            {
                Debug.Assert(_netStream != null, nameof(_netStream) + " != null");
                MemoryStream packet = GetPacket(_netStream);
                IMessage message = MessageBuilder.GetProtoMessage(packet);
                OnReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }
    }
    
    #region RecieveLogic 
    private MemoryStream GetPacket(NetworkStream netStream)
    {
        int packetLength = ReadPacketLength(netStream);
        MemoryStream memStream = new MemoryStream(packetLength);
        memStream.Write(ReadBytesFromNetStream(netStream, packetLength), 0, packetLength);
        memStream.Position = 0;

        return memStream;
    }
    
    private static int ReadPacketLength(NetworkStream stream)
    {
        return BitConverter.ToInt32(ReadBytesFromNetStream(stream, PACKET_COUNT_OF_LENGHT).ReverseIfLittleEndian(), 0);
    }
    
    private static byte[] ReadBytesFromNetStream(NetworkStream netStream, int count)
    {
        byte[] bytes = new byte[count];
        netStream.ReadExactly(bytes, 0, count);
        return bytes;
    }
    
    #endregion
    
    public async Task SendAsync(ProtoMessage item)
    {
        EnsureStreamIsValid();

        using MemoryStream memStream = MessageBuilder.GetStream(item);
        memStream.Position = 0;
        using MemoryStream memStream2 = SetPacketLength(memStream);
        await memStream2.CopyToAsync(_netStream!);
    }

    #region SendLogic

    private MemoryStream SetPacketLength(MemoryStream receivedStream)
    {
        int length = (int)receivedStream.Length;
        byte[] lengthBytes = length.GetBytes();

        MemoryStream newStream = new MemoryStream(PACKET_COUNT_OF_LENGHT + length);
        newStream.Write(lengthBytes, 0, PACKET_COUNT_OF_LENGHT);

        receivedStream.CopyTo(newStream);
        newStream.Position = 0;
        
        return newStream;
    }
    
    private void EnsureStreamIsValid()
    {
        if (_netStream is null)
            throw new NullReferenceException("Stream is null. Call Initialize first!");

        if (!_netStream.CanWrite)
            throw new InvalidOperationException("Stream is not writable.");
    }

    #endregion
    
    public async Task DisconnectAsync()
    {
        await _cancellationSource.CancelAsync();
        if (_receiveTask != null) await _receiveTask;
        _netStream?.Close();
        _client.Close();
        IsInitialized = false;
        OnDisconnected?.Invoke();
    }
    
    public void Dispose()
    {
        _cancellationSource.Dispose();
        DisconnectAsync().Wait();
    }
}