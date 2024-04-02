using MessengerProtocolRealization.Message;
using ReactiveSocketIO.BaseImplementation.Transport;
using ReactiveSocketIO.Core;
using ReactiveSocketIO.Core.Payload;
using ReactiveSocketIO.Core.Message;

namespace ReactiveSocketIO;

public class ReactiveSocket
{

    //Invokes when Handler wasn't registered, to pass the ability to handle from outside
    public event Action<string, SocketResponse> OnRawEventAction;
    
    private int _packetId;
    
    private Dictionary<int, Action<SocketResponse>> _ackActionHandlers;
    private Dictionary<int, Func<SocketResponse, Task>> _ackFuncHandlers;
    private Dictionary<string, Action<SocketResponse>> _eventActionHandlers;
    private Dictionary<string, Func<SocketResponse, Task>?> _eventFuncHandlers;

    private ITransport Transport { get; }

    public ReactiveSocket(ITransport transport) //custom instances of ITransport
    {
        Transport = transport;
        Initialize();
    }
    
    public ReactiveSocket(string connString) //default instance of ITransport with default instance of IMessageBuilder
    {
        Transport = new TcpTransport(connString)
        {
            MessageBuilder = new MessageBuilder(),
        };
        Initialize();
    }
    
    private void Initialize()
    {
        _packetId = 0;
        _ackActionHandlers = new Dictionary<int, Action<SocketResponse>>();
        _ackFuncHandlers = new Dictionary<int, Func<SocketResponse, Task>>();
        _eventActionHandlers = new Dictionary<string, Action<SocketResponse>>();
        _eventFuncHandlers = new Dictionary<string, Func<SocketResponse, Task>?>();
        if(!Transport.IsInitialized)
            Transport.Initialize();
        
        Transport.OnReceived += OnMessageReceived;
    }

    internal async Task ClientAckAsync(int packetId, params IPayload[] data)
    {
        ProtoMessage m = new ProtoMessage()
        {
            Id = packetId,
            Type = MessageType.Ack,
        };
        m.AddPayload(data);
        
        await this.Transport.SendAsync(m).ConfigureAwait(false);
    }

    #region Handlers

    private void EventMessageHandler(IMessage message)
    {
        SocketResponse response = new SocketResponse(message, this);

        if (_eventActionHandlers.TryGetValue(message.Event, out Action<SocketResponse>? handler1))
            handler1.Invoke(response);
        else if (_eventFuncHandlers.TryGetValue(message.Event, out Func<SocketResponse, Task>? handler2))
            handler2?.Invoke(response);
        else
            OnRawEventAction.Invoke(message.Event, response);
    }

    private void AckMessageHandler(IMessage message)
    {
        SocketResponse response = new SocketResponse(message, this);
        if (this._ackActionHandlers.ContainsKey(message.Id))
        {
            this._ackActionHandlers[message.Id].Invoke(response);
            this._ackActionHandlers.Remove(message.Id);
        }
        else
        {
            if (!this._ackFuncHandlers.ContainsKey(message.Id))
                return;
            this._ackFuncHandlers[message.Id].Invoke(response);
            this._ackFuncHandlers.Remove(message.Id);
        }
    }

    #endregion

    #region Event registration

    /// <summary>Register a new handler for the given event.</summary>
    /// <param name="eventName"></param>
    /// <param name="callback"></param>
    public void On(string eventName, Action<SocketResponse> callback)
    {
        if (this._eventActionHandlers.ContainsKey(eventName))
            this._eventActionHandlers.Remove(eventName);
        this._eventActionHandlers.Add(eventName, callback);
    }

    public void On(string eventName, Func<SocketResponse, Task>? callback)
    {
        if (this._eventFuncHandlers.ContainsKey(eventName))
            this._eventFuncHandlers.Remove(eventName);
        this._eventFuncHandlers.Add(eventName, callback);
    }

    #endregion

    #region Emitters

    /// <summary>Emits an event to the socket</summary>
    /// <param name="eventName"></param>
    /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
    /// <returns></returns>
    public async Task EmitAsync(string eventName, params IPayload[] data)
    {
        ProtoMessage m = new ProtoMessage()
        {
            Event = eventName,
            Type = MessageType.Event,
            Id = Interlocked.Increment(ref _packetId),
        };
        m.AddPayload(data);
        
        await this.Transport.SendAsync(m).ConfigureAwait(false);
    }

    private async Task EmitAsyncForAck(string eventName, int packetId, params IPayload[] data)
    {
        ProtoMessage m = new ProtoMessage()
        {
            Event = eventName,
            Type = MessageType.Event,
            Id = packetId,
        };
        m.AddPayload(data);
        
        await this.Transport.SendAsync(m).ConfigureAwait(false);
    }

    /// <summary>Emits an event to the socket</summary>
    /// <param name="eventName"></param>
    /// <param name="ack">will be called with the server answer.</param>
    /// <param name="data">Any other parameters can be included. All serializable datastructures are supported, including byte[]</param>
    /// <returns></returns>
    
    // private async Task EmitAsyncForAck(string eventName, params IPayload[] data)
    // {
    //     ProtoMessage m = new ProtoMessage()
    //     {
    //         Event = eventName,
    //         Type = MessageType.Ack,
    //         Id = Interlocked.Increment(ref _packetId),
    //     };
    //     m.AddPayload(data);
    //     
    //     await this.Transport.SendAsync(m).ConfigureAwait(false);
    // }
    
    public async Task EmitAsync(string eventName, Action<SocketResponse> ack, params IPayload[] data)
    {
        try
        {
            int packetId = Interlocked.Increment(ref _packetId);
            _ackActionHandlers.TryAdd(packetId, ack);
            await EmitAsyncForAck(eventName, packetId, data).ConfigureAwait(false);
        }
        catch
        {
            //TODO: 
        }
    }
    
    public async Task EmitAsync(string eventName, Func<SocketResponse, Task> ack, params IPayload[] data)
    {
        try
        {
            int packetId = Interlocked.Increment(ref _packetId);
            _ackFuncHandlers.TryAdd(packetId, ack);
            await EmitAsyncForAck(eventName, packetId, data).ConfigureAwait(false);
        }
        catch
        {
            //TODO: 
        }
    }

    #endregion
    
    
    private void OnMessageReceived(IMessage msg)
    {
        switch (msg.Type)
        {
            case MessageType.Connected:
                //this.ConnectedHandler(msg);
                break;
            case MessageType.Disconnected:
                //this.DisconnectedHandler();
                break;
            case MessageType.Event:
                EventMessageHandler(msg);
                break;
            case MessageType.Ack:
                AckMessageHandler(msg);
                break;
            case MessageType.Error:
                //this.ErrorMessageHandler(msg);
                break;
        }
    }
}