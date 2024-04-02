namespace ReactiveSocketIO.Core.Payload;

public interface IReversable
{
    static abstract T GetObj<T>(Stream stream);
}