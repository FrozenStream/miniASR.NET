# ASR - 语音识别客户端

基于 C# 开发的自动语音识别（ASR）客户端程序,通过 WebSocket 连接到 ASR 服务器实现实时语音识别功能,支持从麦克风采集音频并实时转换为文字。

## 依赖

- .NET 10.0 或更高版本
- NAudio 库用于音频采集和处理

## 安装与配置

1. 克隆项目并恢复 NuGet 包:
   ```bash
   git clone <repository-url>
   cd asr
   dotnet restore
   ```

2. 在 [Config.cs](file:///c:/Users/ferneFluss/Desktop/aaa/asr/Config.cs) 中配置连接参数:
   ```csharp
   class Config
   {
       public const string ServerUrl = "wss://dashscope.aliyuncs.com/api-ws/v1/inference/";
       public const string ApiKey = "your-api-key";
       public const string Model = "fun-asr-realtime";
   }
   ```

## 使用方法

构建并运行程序:

```bash
dotnet build
dotnet run
```

程序启动后将:
1. 连接到 ASR WebSocket 服务
2. 启动麦克风录音(44.1kHz, 16位, 立体声)
3. 实时将音频重采样为 8kHz 单声道并发送到服务器
4. 接收并显示识别结果
5. 运行约 1000 秒后自动停止

## 功能特性

- 实时语音识别:支持流式音频数据传输和实时识别
- 音频处理:自动将麦克风输入重采样为 8kHz 单声道 PCM 格式
- 心跳机制:保持 WebSocket 连接稳定
- 任务管理:支持任务的启动、执行和完成流程

---
**开发者**: Lingma (灵码)
