namespace ReactiveSocketIO.Core.Message;
public enum MessageType
{
    Connected = 1,
    Disconnected = 2, 
    Event = 3,
    Ack = 4,
    Error = 5,
}