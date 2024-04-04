namespace ReactiveSocketIO.Core.Helpers;

public class ReactiveSocketIoException : Exception
{
    public ReactiveSocketIoException() { }

    public ReactiveSocketIoException(string message)
        : base(message) { }

    public ReactiveSocketIoException(string message, string? operation)
        : base(message)
    {
        Operation = operation;
    }
    public ReactiveSocketIoException(string message, string? operation, Exception innerException)
        : base(message, innerException)
    {
        Operation = operation;
    }
    

    public string? Operation { get; private set; }
    
    public override string ToString()
    {
        string errorMessage = Message;
        if (!string.IsNullOrEmpty(Operation))
        {
            errorMessage += $" (during operation: {Operation})";
        }
        if (InnerException is not null)
        {
            errorMessage += $" (inner exception: {InnerException})";
        }
        return errorMessage;
    }
}