using NAudio.Wave;
using System.Text.Json;
using System.Text.Json.Nodes;

string _taskId = "";

var callback = new Callback();
var client = new Client(Config.ServerUrl, Config.ApiKey, callback);
var _taskStartedTcsDict = new Dictionary<string, TaskCompletionSource<bool>>();
callback.MessageStream.Subscribe(response =>
{
    var message = JsonNode.Parse(response);
    if (message != null)
    {
        var eventValue = message["header"]?["event"]?.GetValue<string>();
        switch (eventValue)
        {
            case "task-started":
                Console.WriteLine("任务开启成功");
                _taskStartedTcsDict[_taskId].SetResult(true);
                break;
            case "result-generated":
                Console.WriteLine($"识别结果：{message["payload"]?["output"]?["sentence"]?["text"]?.GetValue<string>()}");
                if (message["payload"]?["usage"] != null && message["payload"]?["usage"]?["duration"] != null)
                {
                    Console.WriteLine($"任务计费时长（秒）：{message["payload"]?["usage"]?["duration"]?.GetValue<int>()}");
                }
                break;
            case "task-finished":
                Console.WriteLine("任务完成");
                client.Cancel();
                break;
            case "task-failed":
                Console.WriteLine($"任务失败：{message["header"]?["error_message"]?.GetValue<string>()}");
                client.Cancel();
                break;
        }
    }
});

string CreateCommand(string action, string taskId, string streaming, object payload)
{
    var command = new
    {
        header = new
        {
            action,
            task_id = taskId,
            streaming
        },
        payload
    };

    return JsonSerializer.Serialize(command);
}

async Task SendRunTaskCommandAsync(string taskId)
{
    var command = CreateCommand("run-task", taskId, "duplex", new
    {
        task_group = "audio",
        task = "asr",
        function = "recognition",
        model = Config.Model,
        parameters = new
        {
            format = "pcm",
            sample_rate = 8000,
            heartbeat = true
        },
        input = new { }
    });

    await client.SendMessageAsync(command);
    Console.WriteLine("已发送run-task指令。");
}

async Task SendFinishTaskCommandAsync(string taskId)
{
    var command = CreateCommand("finish-task", taskId, "duplex", new
    {
        input = new { }
    });

    await client.SendMessageAsync(command);
    Console.WriteLine("已发送finish-task指令。");
}

string GenerateTaskId()
{
    return Guid.NewGuid().ToString("N")[..32];
}

_taskId = GenerateTaskId();
_taskStartedTcsDict[_taskId] = new();

// 连接WebSocket服务
await client.ConnectToWebSocketAsync();

// 启动接收消息的任务
Task receiveTask = client.ReceiveMessagesAsync();

// 发送run-task指令
await SendRunTaskCommandAsync(_taskId);

// 等待task-started指令
await _taskStartedTcsDict[_taskId].Task;
_taskStartedTcsDict.Remove(_taskId);

Console.WriteLine("开始实时读取麦克风...");

// 使用默认录音设备，设置录音格式为44.1kHz, 16位, 立体声
var waveIn = new WaveInEvent
{
    DeviceNumber = 0,
    WaveFormat = new WaveFormat(44100, 16, 2)
};

Console.WriteLine($"录音格式: {waveIn.WaveFormat.SampleRate}Hz, {waveIn.WaveFormat.BitsPerSample}位, {waveIn.WaveFormat.Channels}声道");
Console.WriteLine($"输出格式: 8000Hz, 16位, 1声道");

// 处理录音数据
var bytesPer100Ms = 8000 * 2 * 1 / 10;
var audioBuffer = new ByteRingBuffer(bytesPer100Ms * 2);

waveIn.DataAvailable += async (s, a) =>
{
    // 转换为float数组进行处理，然后转回byte数组写入文件
    float[] floatSamples = Utils.ConvertToFloatArray(a.Buffer, a.BytesRecorded, waveIn.WaveFormat.Channels);
    float[] resampledFloatSamples = Utils.ResampleTo8kMono(floatSamples, waveIn.WaveFormat.SampleRate, waveIn.WaveFormat.Channels);
    byte[] outputBuffer = Utils.ConvertToByteArray(resampledFloatSamples);

    audioBuffer.EnqueueSpan(outputBuffer);

    // 如果缓冲区积累了足够100ms的数据，则发送
    while (audioBuffer.Count >= bytesPer100Ms)
    {
        var dataToSend = new byte[bytesPer100Ms];
        audioBuffer.DequeueSpan(dataToSend);
        await client.SendBinaryAsync(dataToSend);
    }
};

Console.WriteLine("正在启动录音，请说话...");
waveIn.StartRecording();

await Task.Delay(1000000);

waveIn.StopRecording();

// 发送finish-task指令
await SendFinishTaskCommandAsync(_taskId);

// 等待接收任务完成
await receiveTask;