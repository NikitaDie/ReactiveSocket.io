namespace ReactiveSocketIO.Core.Payload;

public interface IPayload
{
    public MemoryStream GetStream();

    public Type GetPayloadType();
}