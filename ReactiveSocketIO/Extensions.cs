namespace ReactiveSocketIO;

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
}