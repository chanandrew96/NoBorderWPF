using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace NoBorderWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "NoBorderWPF_SingleInstance";
        private const string PipeName = "NoBorderWPF_Pipe";
        private Mutex? _mutex;
        private NamedPipeServerStream? _pipeServer;
        private bool _isFirstInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 創建 Mutex
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);

            if (_isFirstInstance)
            {
                // 第一個實例：啟動命名管道伺服器
                StartNamedPipeServer();
                base.OnStartup(e);
            }
            else
            {
                // 非第一個實例：發送訊息並退出
                SendShowWindowMessage();
                Shutdown();
            }
        }

        private void StartNamedPipeServer()
        {
            // 在背景執行緒啟動命名管道伺服器
            Thread pipeThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
                        {
                            pipeServer.WaitForConnection();
                            using (var reader = new StreamReader(pipeServer))
                            {
                                string? message = reader.ReadLine();
                                if (message == "ShowWindow")
                                {
                                    // 在 UI 執行緒上顯示視窗
                                    Dispatcher.Invoke(() =>
                                    {
                                        if (MainWindow != null)
                                        {
                                            MainWindow.Visibility = Visibility.Visible;
                                            MainWindow.Activate();
                                        }
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤（可選）
                        System.Diagnostics.Debug.WriteLine($"Pipe server error: {ex.Message}");
                    }
                }
            })
            {
                IsBackground = true
            };
            pipeThread.Start();
        }

        private void SendShowWindowMessage()
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000); // 等待 1 秒
                    using (var writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("ShowWindow");
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤（可選）
                System.Diagnostics.Debug.WriteLine($"Pipe client error: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 清理 Mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }

}
