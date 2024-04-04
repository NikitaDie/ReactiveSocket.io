using System.Diagnostics;

namespace ReactiveSocketIO.Core.Helpers;

public static class Extensions
{
    public static byte[] ReverseIfLittleEndian(this byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }
    
    public static byte[] GetBytes(this int val)
    {
        byte[] intBytes = BitConverter.GetBytes(val);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);

        return intBytes;
    }

    public static string? GetMethodName()
    {
        StackTrace stackTrace = new StackTrace();
        string? methodName = stackTrace.GetFrame(1)?.GetMethod()?.Name;
        return methodName;
    }
}