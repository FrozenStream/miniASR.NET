using System.Reactive.Linq;
using System.Reactive.Subjects;

public class Callback : IDisposable
{
    private readonly Subject<string> _textSubject = new();
    private readonly Subject<ArraySegment<byte>> _binarySubject = new();
    private readonly Subject<string> _errorSubject = new();

    // 暴露只读的Observable接口
    public IObservable<string> MessageStream => _textSubject.AsObservable();
    public IObservable<ArraySegment<byte>> BinaryStream => _binarySubject.AsObservable();
    public IObservable<string> ErrorStream => _errorSubject.AsObservable();

    // 用于发送数据的方法
    public void OnText(string text) => _textSubject.OnNext(text);
    public void OnBinary(ArraySegment<byte> binary) => _binarySubject.OnNext(binary);
    public void OnError(string error) => _errorSubject.OnNext(error);

    public void Complete()
    {
        _textSubject.OnCompleted();
        _binarySubject.OnCompleted();
        _errorSubject.OnCompleted();
    }

    public void Dispose()
    {
        _textSubject.Dispose();
        _binarySubject.Dispose();
        _errorSubject.Dispose();
        GC.SuppressFinalize(this);
    }
}
