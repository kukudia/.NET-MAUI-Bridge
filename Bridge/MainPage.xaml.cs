namespace Bridge
{
    public partial class MainPage : ContentPage
    {
        private async void OnSendClicked(object sender, EventArgs e)
        {
            // 1. 检查目标 IP 地址是否有效 / Check if the target IP address is valid
            var ip = TargetIpEntry.Text.Trim();
            if (string.IsNullOrEmpty(ip) || !System.Net.IPAddress.TryParse(ip, out _))
            {
                await DisplayAlert("错误 / Error", "请输入有效的目标 IP 地址 / Please enter a valid target IP address.", "OK");
                return;
            }

            // 2. 调用 MAUI 文件选择器 API / Call MAUI File Picker API
            var pickResult = await FilePicker.Default.PickAsync();

            if (pickResult == null)
            {
                StatusLabel.Text = "已取消选择文件 / File selection cancelled.";
                TransferProgress.Progress = 0;
                return;
            }

            // 3. 准备传输对象和进度汇报 / Prepare transfer object and progress reporter
            var service = new TransferService();

            // 创建 IProgress 对象，用于监听 SendFileAsync 报告的进度 / Create IProgress object to listen for progress reported by SendFileAsync
            var progressReporter = new Progress<double>(p =>
            {
                // 必须在主线程上更新 UI，以避免线程安全问题 / Must update UI on the Main Thread to avoid thread safety issues
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TransferProgress.Progress = p;
                    // 将 p 格式化为百分比 (P0) / Format p as percentage (P0)
                    StatusLabel.Text = $"正在发送: {Path.GetFileName(pickResult.FullPath)} ({p:P0})";
                });
            });

            // 禁用按钮，避免在传输过程中重复点击 / Disable buttons to prevent double-clicking during transfer
            (sender as Button).IsEnabled = false;
            ReceiveBtn.IsEnabled = false;

            try
            {
                // 4. 开始发送 / Start sending
                StatusLabel.Text = "正在尝试连接并发送... / Attempting to connect and send...";

                await service.SendFileAsync(ip, pickResult.FullPath, progressReporter);

                StatusLabel.Text = $"文件发送成功! / File sent successfully!";
            }
            catch (Exception ex)
            {
                // 5. 错误处理和显示 / Error handling and display
                StatusLabel.Text = $"发送失败: {ex.Message} / Sending failed: {ex.Message}";
            }
            finally
            {
                // 6. 恢复 UI 状态 / Restore UI state
                (sender as Button).IsEnabled = true;
                ReceiveBtn.IsEnabled = true;
                TransferProgress.Progress = 0;
            }
        }

        private void OnReceiveClicked(object sender, EventArgs e)
        {
            // 接收逻辑暂时留空 / Receive logic is left empty for now
            StatusLabel.Text = "请先实现接收逻辑 / Please implement receive logic first.";
        }
    }
}
