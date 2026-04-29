using System.Net.WebSockets;
using System.Text;

public class Client(string serverUrl, string apikey, Callback callback)
{
    private ClientWebSocket _webSocket = new();
    private readonly string _serverUrl = serverUrl;
    private readonly string _apikey = apikey;
    private readonly Callback _callback = callback;
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _readytofinish = false;

    public async Task ConnectToWebSocketAsync()
    {
        _cancellationTokenSource = new();
        _readytofinish = false;
        // 如果WebSocket已经是连接或正在连接的状态，直接返回
        if (_webSocket.State is WebSocketState.Connecting or WebSocketState.Open) return;

        // 如果WebSocket处于非活跃状态，创建一个新的实例
        if (_webSocket.State is WebSocketState.Closed or WebSocketState.Aborted)
            _webSocket = new ClientWebSocket();

        // 设置WebSocket连接的头部信息
        _webSocket.Options.SetRequestHeader("Authorization", $"bearer {_apikey}");
        _webSocket.Options.SetRequestHeader("X-DashScope-DataInspection", "enable");

        try
        {
            var uri = new Uri(_serverUrl);
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            Console.WriteLine("[Client]: Successfully connected to WebSocket service.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Client]: WebSocket connection was cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client]: WebSocket connection failed: {ex.Message}");
            throw;
        }
    }

    public async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            try
            {
                var buffer = new byte[1024 * 4];

                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("[Client]: Received close message, ready to finish...");
                    _readytofinish = true;
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Process binary data
                    Console.WriteLine($"[Client]: Received binary message with byte length {result.Count}...");
                    _callback.OnBinary(new ArraySegment<byte>(buffer, 0, result.Count));
                }
                else
                {
                    Console.WriteLine($"[Client]: Received text message with byte length {result.Count}...");
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _callback.OnText(message);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Client]: Receiving messages cancelled.");
                return;
            }
        }
    }

    public async Task SendMessageAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        // Console.WriteLine($"[Client]: Sending text message with bytes length: {buffer.Length}...");
        try
        {
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Client]: Message sending was cancelled.");
        }
    }

    public async Task SendBinaryAsync(byte[] binary)
    {
        // Console.WriteLine($"[Client]: Sending binary message with bytes length: {binary.Length}...");
        try
        {
            await _webSocket.SendAsync(binary, WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Client]: Message sending was cancelled.");
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public async Task CloseAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        _cancellationTokenSource.Cancel();
    }
}