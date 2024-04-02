using System.Diagnostics.CodeAnalysis;
using ReactiveSocketIO.Core.Message;

namespace ReactiveSocketIO.BaseImplementation.Message;

[SuppressMessage("ReSharper", "InconsistentNaming")] 
public class MessageBuilder : IMessageBuilder
{
    public const char HEADER_SEPARATOR = ':';
    public const string HEADER_PAYLOAD_LEN = "len";
    public const string PAYLOAD_SEPARATOR = "--payload";
    
    public MemoryStream GetStream(ProtoMessage pm)
    {
        MemoryStream memStream = new MemoryStream();
        using StreamWriter writer = new StreamWriter(memStream, leaveOpen:true);
            
        // 1. Write Id, Type, Event
        writer.WriteLine(pm.Id);
        writer.WriteLine(pm.Type);
        writer.WriteLine(pm.Event);
                
        // 2. Write Headers
        foreach (KeyValuePair<string, string> h in pm.Headers)
            writer.WriteLine($"{h.Key}:{h.Value}");
        writer.WriteLine();

        // 3. Write Payloads
        foreach (var payloadInfo in pm.PayloadsInfo)
        {
            writer.WriteLine($"{PAYLOAD_SEPARATOR}:{payloadInfo.Type}");
            writer.Flush();
            payloadInfo.Stream.Position = 0;
            payloadInfo.Stream.CopyTo(memStream);
            writer.Write('\n');
        }
        writer.Flush();
        
        return memStream;
    }

    public ProtoMessage GetProtoMessage(MemoryStream memStream)
    {
        ProtoMessage protoMessage  = new ProtoMessage();
        
        using StreamReader reader = new StreamReader(memStream);
        ReadMetadata(protoMessage, reader);
        ReadPayload(protoMessage, reader);

        return protoMessage;
    }

    #region Readers

    private static void ReadMetadata(ProtoMessage pm, StreamReader sr)
    {
        sr.BaseStream.Position = 0;

        pm.Id = Convert.ToInt32(sr.ReadLine());
        // getting Message Type
        Enum.TryParse(sr.ReadLine(), out MessageType type);
        pm.Type = type;
        pm.Event = sr.ReadLine();
        
        string? headerLine;
        while(! string.IsNullOrEmpty(headerLine = sr.ReadLine()))
            pm.SetHeader(headerLine);
    }

    private static void ReadPayload(ProtoMessage protoMessage, StreamReader reader)
    {
        List<MemoryStream> payloadStreams = new List<MemoryStream>();
        string? currentPayloadType = null;
        MemoryStream? currentPayloadStream = null;

        while (reader.ReadLine() is { } line)
        {
            // If the current line is the PAYLOAD_SEPARATOR, it means a new payload is starting.
            if (line.Contains(ProtoMessage.PAYLOAD_SEPARATOR))
            {
                // 1. Add the current payload stream and Info
                if (currentPayloadStream != null && currentPayloadType != null)
                {
                    payloadStreams.Add(currentPayloadStream);
                    protoMessage.PayloadsInfo.Add(new PayloadInfo
                    {
                        Type = currentPayloadType, 
                        Stream = currentPayloadStream,
                    });
                }
                
                // 2. Read the next line to get the type of the new payload 
                currentPayloadType = line.Split(ProtoMessage.HEADER_SEPARATOR, 
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
                currentPayloadStream = new MemoryStream();
            }
            else
            {
                //If the current line is not a separator, it means it contains payload data.
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(line + "\n");
                currentPayloadStream?.Write(buffer, 0, buffer.Length);
            }
        }
        
        // Add the last payload stream
        if (currentPayloadStream != null && currentPayloadType != null)
        {
            payloadStreams.Add(currentPayloadStream);
            protoMessage.PayloadsInfo.Add(new PayloadInfo
            {
                Type = currentPayloadType, 
                Stream = currentPayloadStream,
            });
        }
        
        // Update the payload length header
        protoMessage.Headers[ProtoMessage.HEADER_PAYLOAD_LEN] = payloadStreams.Sum(ps => (int)ps.Length).ToString();
    }

    #endregion
    
}