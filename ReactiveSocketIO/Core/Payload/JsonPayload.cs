using System.Text;
using System.Text.Json;

namespace ReactiveSocketIO.Core.Payload;

public abstract class JsonPayload : IPayload, IReversable
{
    public MemoryStream GetStream()
    {
        MemoryStream memStream = new MemoryStream();

        string json = GetJson();

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        memStream.Write(bytes, 0, bytes.Length);

        return memStream;
    }

    public abstract Type GetPayloadType();

    public static T GetObj<T>(Stream stream)
    {
        stream.Position = 0;
        string s;
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        { 
            s = reader.ReadToEnd();
        }

        return JsonSerializer.Deserialize<T>(s);
    }

    protected string GetJson()
    {
        return JsonSerializer.Serialize(this, this.GetType());
    }
    
}