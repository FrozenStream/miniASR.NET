public class Utils
{
    /// <summary>
    /// 生成WAV文件头（44字节）
    /// </summary>
    public static byte[] GenerateWavHeader(int sampleRate, int bitsPerSample, int channels)
    {
        byte[] header = new byte[44];

        // RIFF标识符
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, header, 0, 4);
        
        // 文件大小（稍后填充，这里先设为0）
        Array.Copy(BitConverter.GetBytes(0), 0, header, 4, 4);
        
        // WAVE标识符
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, header, 8, 4);
        
        // fmt子块
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, header, 12, 4);
        
        // fmt子块大小（16）
        Array.Copy(BitConverter.GetBytes(16), 0, header, 16, 4);
        
        // 音频格式（1 = PCM）
        Array.Copy(BitConverter.GetBytes((short)1), 0, header, 20, 2);
        
        // 声道数
        Array.Copy(BitConverter.GetBytes((short)channels), 0, header, 22, 2);
        
        // 采样率
        Array.Copy(BitConverter.GetBytes(sampleRate), 0, header, 24, 4);
        
        // 字节率（采样率 * 声道数 * 位深/8）
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        Array.Copy(BitConverter.GetBytes(byteRate), 0, header, 28, 4);
        
        // 块对齐（声道数 * 位深/8）
        short blockAlign = (short)(channels * bitsPerSample / 8);
        Array.Copy(BitConverter.GetBytes(blockAlign), 0, header, 32, 2);
        
        // 位深
        Array.Copy(BitConverter.GetBytes((short)bitsPerSample), 0, header, 34, 2);
        
        // data子块
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("data"), 0, header, 36, 4);
        
        // data子块大小（稍后填充，这里先设为0）
        Array.Copy(BitConverter.GetBytes(0), 0, header, 40, 4);

        return header;
    }

    /// <summary>
    /// 将音频数据重采样到8kHz单声道（在float域内操作）
    /// </summary>
    public static float[] ResampleTo8kMono(float[] inputSamples, int inputSampleRate, int inputChannels)
    {
        double ratio = (double)inputSampleRate / 8000; // 源采样率与目标采样率之比
        int outputSampleCount = (int)(inputSamples.Length / ratio); // 输出样本数
        float[] outputSamples = new float[outputSampleCount]; // 单声道输出

        for (int i = 0; i < outputSampleCount; i++)
        {
            double srcIndex = i * ratio;
            int index0 = (int)Math.Floor(srcIndex);
            int index1 = Math.Min(index0 + 1, inputSamples.Length - 1);
            float fraction = (float)(srcIndex - index0);

            // 线性插值
            outputSamples[i] = inputSamples[index0] * (1 - fraction) + inputSamples[index1] * fraction;
        }

        return outputSamples;
    }

    /// <summary>
    /// 将字节数组转换为float数组 (-1.0f ~ 1.0f)
    /// </summary>
    public static float[] ConvertToFloatArray(byte[] inputBuffer, int bytesRecorded, int channels)
    {
        int bytesPerSample = 2; // 16位 = 2字节
        int sampleCount = bytesRecorded / bytesPerSample;

        // 我们只需要一半的样本数（如果是立体声，则只取左声道）
        float[] samples = new float[sampleCount / channels];

        for (int i = 0; i < sampleCount; i += channels)
        {
            // 只取左声道（索引i）并将16位整型(-32768~32767)转换为float(-1.0f~1.0f)
            short intValue = BitConverter.ToInt16(inputBuffer, i * 2);
            samples[i / channels] = intValue / 32768.0f;
        }

        return samples;
    }

    /// <summary>
    /// 将float数组(-1.0f ~ 1.0f)转换为字节数组(16位整型)
    /// </summary>
    public static byte[] ConvertToByteArray(float[] floatSamples)
    {
        byte[] outputBuffer = new byte[floatSamples.Length * 2]; // 16位 = 2字节

        for (int i = 0; i < floatSamples.Length; i++)
        {
            // 将float(-1.0f~1.0f)转换为16位整型(-32768~32767)
            short intValue = (short)(Math.Max(-1.0f, Math.Min(1.0f, floatSamples[i])) * 32767);
            Array.Copy(BitConverter.GetBytes(intValue), 0, outputBuffer, i * 2, 2);
        }

        return outputBuffer;
    }
}