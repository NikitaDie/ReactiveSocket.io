using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ReactiveSocketIO.Core.Helpers;
using ReactiveSocketIO.Core.Payload;

namespace ReactiveSocketIO.Core.Message
{
    public class PayloadInfo
    {
        public required string Type { get; init; }
        public required MemoryStream Stream { get; init; }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")] 
    public class ProtoMessage : IMessage 
    {
        public const char HEADER_SEPARATOR = ':';
        public const string HEADER_PAYLOAD_LEN = "len";
        public const string PAYLOAD_SEPARATOR = "--payload";
        
        public string? Event { get; set; }
        public MessageType Type { get; set; }
        public int Id { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public List<PayloadInfo> PayloadsInfo { get; private set; } = new List<PayloadInfo>();
        
        public int PayloadLength
        {
            get
            {
                Headers.TryGetValue(HEADER_PAYLOAD_LEN, out string? value);
        
                if (string.IsNullOrEmpty(value))
                    return 0;
        
                return Convert.ToInt32(value);
            }
        }
        
        #region Headers
        
        public void SetHeader(string key, string value)
            => Headers[key] = value;
        

        public void SetHeader(string header)
        {
            string[] chunks = header.Split(HEADER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            if (chunks.Length >= 2)
                SetHeader(chunks[0], chunks[1]);
            
        }
        
        #endregion

        #region Payloads
        
        public void CleanPayloadCollections()
        {
            PayloadsInfo.Clear();
        }
        
        public void AddPayload(IPayload payload)
        {
            PayloadsInfo.Add(new PayloadInfo 
            { 
                Type = payload.GetPayloadType().ToString(), 
                Stream = payload.GetStream(),
            });
            Headers[HEADER_PAYLOAD_LEN] = PayloadsInfo.Sum(pi => (int)pi.Stream.Length).ToString();
        }

        public void AddPayload(IEnumerable<IPayload> payloads)
        {
            PayloadsInfo.AddRange(payloads.Select(p => new PayloadInfo
            {
                Type = p.GetPayloadType().ToString(), 
                Stream = p.GetStream(),
            }));
            Headers[HEADER_PAYLOAD_LEN] = PayloadsInfo.Sum(pi => (int)pi.Stream.Length).ToString();
        }
        
        public void AddPayload(params IPayload[] payloads)
        {
            PayloadsInfo.AddRange(payloads.Select(p => new PayloadInfo
            { 
                Type = p.GetPayloadType().ToString(), 
                Stream = p.GetStream()
            }));
            Headers[HEADER_PAYLOAD_LEN] = PayloadsInfo.Sum(pi => (int)pi.Stream.Length).ToString();
        }
        
        #endregion

        #region IMessage

        public int PayloadCount => PayloadsInfo.Count;
        
        public T GetValue<T>(int index)
            where T : IReversable
        {
            if (index >= PayloadCount)
                throw new ReactiveSocketIoException("Index of Payload is out of range!", Extensions.GetMethodName(), new IndexOutOfRangeException());
            
            // Get the type of T
            Type? currentType = typeof(T);
                
            // Loop through the inheritance hierarchy
            while (currentType != null)
            {
                // Look for the GetObj method in the current type
                MethodInfo? method = currentType.GetMethod("GetObj", BindingFlags.Public | BindingFlags.Static);

                if (method != null)
                {
                    MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
                    MemoryStream pStream = PayloadsInfo[index].Stream;
                    return (T)genericMethod.Invoke(null, new object[] { pStream });
                }

                // Move to the base type for next iteration
                currentType = currentType.BaseType;
            }

            // If not found in any ancestor, throw an exception
            throw new ReactiveSocketIoException(
                $"Type '{typeof(T)}' or its ancestors do not have a public static 'GetObj' method!",
                Extensions.GetMethodName());
        }

        public string GetPayloadType(int index)
        {
            if (index >= PayloadCount)
                throw new ReactiveSocketIoException("Index of Payload is out of range!", Extensions.GetMethodName(), new IndexOutOfRangeException());
            
            return PayloadsInfo[index].Type;
        }
        
        #endregion
        
    }
}
