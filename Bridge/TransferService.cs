using System.Net.Sockets;
using System.Text;
using System.Net;

namespace Bridge;

public class TransferService
{
    // 定义默认端口号，发送方和接收方必须一致 / Define the default port number, must be the same on sender and receiver
    private const int Port = 12345;

    /// <summary>
    /// 异步发送文件到指定的 IP 地址。
    /// Asynchronously sends a file to the specified IP address.
    /// </summary>
    /// <param name="ipAddress">目标接收方的 IP 地址 / Target receiver's IP address.</param>
    /// <param name="filePath">本地文件的完整路径 / Full path of the local file.</param>
    /// <param name="progress">用于报告传输进度的接口 / Interface for reporting transfer progress.</param>
    public async Task SendFileAsync(string ipAddress, string filePath, IProgress<double> progress)
    {
        // 1. 文件存在性检查 / File existence check
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件未找到: {filePath} / File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        var fileName = fileInfo.Name;
        var fileSize = fileInfo.Length;

        // 使用 using 确保 TcpClient 资源被正确释放 / Use 'using' to ensure TcpClient resources are correctly disposed
        using var client = new TcpClient();

        try
        {
            // 2. 建立 TCP 连接 / Establish TCP Connection
            // 使用 ConnectAsync 避免阻塞 UI 线程 / Use ConnectAsync to avoid blocking the UI thread
            await client.ConnectAsync(IPAddress.Parse(ipAddress), Port);
            using var networkStream = client.GetStream(); // 获取网络数据流 / Get the network data stream

            // --- 3. 构造并发送文件头部 (头部协议) / Construct and Send File Header (Header Protocol) ---
            // 协议顺序: [文件名长度: 4 bytes] -> [文件名: N bytes] -> [文件大小: 8 bytes]
            // Protocol Order: [Filename Length: 4 bytes] -> [Filename: N bytes] -> [File Size: 8 bytes]

            var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            var fileNameLenBytes = BitConverter.GetBytes(fileNameBytes.Length); // Int32 -> 4 bytes
            var fileLenBytes = BitConverter.GetBytes(fileSize);                // Int64 -> 8 bytes

            // 写入头部信息到网络流 / Write header info to the network stream
            await networkStream.WriteAsync(fileNameLenBytes); // 发送文件名长度 / Send filename length
            await networkStream.WriteAsync(fileNameBytes);    // 发送文件名 / Send filename
            await networkStream.WriteAsync(fileLenBytes);     // 发送文件大小 / Send file size

            // --- 4. 发送文件内容 / Send File Content ---
            using var fileStream = fileInfo.OpenRead();
            var buffer = new byte[81920]; // 80 KB 缓冲区 / 80 KB buffer size
            int bytesRead;
            long totalSent = 0; // 累计已发送字节数 / Accumulated bytes sent

            // 循环读取本地文件，并写入网络流 / Loop reading local file and writing to network stream
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await networkStream.WriteAsync(buffer, 0, bytesRead);
                totalSent += bytesRead;

                // 报告进度 / Report progress
                double progressValue = (double)totalSent / fileSize;
                progress.Report(progressValue);
            }
        }
        catch (SocketException ex)
        {
            // 捕获 Socket 相关的网络错误，例如连接被拒绝、超时等 / Catch Socket related network errors, e.g., connection refused, timeout
            throw new Exception($"连接失败或网络错误，请检查 IP 和接收方是否已开启监听 / Connection failed or network error. Check IP and if receiver is listening: {ex.Message}");
        }
        finally
        {
            // 确保任务完成后进度条设置为 100% / Ensure progress is set to 100% upon task completion
            progress.Report(1.0);
        }
    }
}