using System.Buffers;

public sealed class ByteRingBuffer(int capacity) : IDisposable
{
    private readonly byte[] _buffer = ArrayPool<byte>.Shared.Rent(capacity);
    private int _readIndex = 0;
    private int _writeIndex = 0;
    private int _count = 0;
    private readonly int _capacity = capacity;

    public int Count => _count;
    public bool IsEmpty => _count == 0;
    public bool IsFull => _count == _capacity;

    public void Enqueue(byte data)
    {
        if (IsFull)
            throw new InvalidOperationException("Buffer is full");

        _buffer[_writeIndex] = data;
        _writeIndex = (_writeIndex + 1) % _capacity;
        _count++;
    }

    public byte Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Buffer is empty");

        byte data = _buffer[_readIndex];
        _readIndex = (_readIndex + 1) % _capacity;
        _count--;
        return data;
    }

    // 批量写入（高效）
    public void EnqueueSpan(ReadOnlySpan<byte> data)
    {
        if (data.Length > _capacity - _count)
            throw new InvalidOperationException("Not enough space");

        int firstPart = Math.Min(data.Length, _capacity - _writeIndex);
        data.Slice(0, firstPart).CopyTo(_buffer.AsSpan(_writeIndex));
        if (firstPart < data.Length)
        {
            data.Slice(firstPart).CopyTo(_buffer.AsSpan(0));
        }

        _writeIndex = (_writeIndex + data.Length) % _capacity;
        _count += data.Length;
    }

    // 批量读取（高效）
    public void DequeueSpan(Span<byte> destination)
    {
        if (destination.Length > _count)
            throw new InvalidOperationException("Not enough data");

        int firstPart = Math.Min(destination.Length, _capacity - _readIndex);
        _buffer.AsSpan(_readIndex, firstPart).CopyTo(destination.Slice(0, firstPart));
        if (firstPart < destination.Length)
        {
            _buffer.AsSpan(0, destination.Length - firstPart)
                   .CopyTo(destination.Slice(firstPart));
        }

        _readIndex = (_readIndex + destination.Length) % _capacity;
        _count -= destination.Length;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
    }
}