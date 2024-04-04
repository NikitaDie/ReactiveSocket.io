using ReactiveSocketIO.Core.Message;

namespace ReactiveSocketIO.Core.Transport;

public interface ITransport : IDisposable
{
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<IMessage> OnReceived;
    public event Action<Exception> OnError;
    
    public bool IsInitialized { get; }

    void Initialize();
    Task SendAsync(ProtoMessage items);
    Task DisconnectAsync();
}