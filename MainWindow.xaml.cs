using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using H.NotifyIcon; // H.NotifyIcon.Wpf 2.3.1

namespace NoBorderWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> messages = new List<string>();
        private int currentMessageIndex = 0;
        private string txtFilePath = @"C:\Users\ChanCheH\.vscode\extensions\mathon.code-novel-0.0.6\books\13_2.txt"; // TXT 檔案路徑
        private string indexFilePath = @"C:\Users\ChanCheH\.vscode\extensions\mathon.code-novel-0.0.6\books\lastIndex.txt"; // 儲存上次索引的檔案路徑
        private double maxTextWidth; // 用於計算文字裁剪
        private TaskbarIcon? notifyIcon;
        private readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoBorderWPF",
            "config.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadMessages();
            LoadLastIndex();
            UpdateMessage();
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.ForceCreate();
        }

        public void SwitchTxtSource(string newFilePath)
        {
            // 保存當前檔案的索引
            SaveLastIndex();
            // 更新檔案路徑
            txtFilePath = newFilePath;
            indexFilePath = Path.Combine(Path.GetDirectoryName(txtFilePath)!, "lastIndex_" + Path.GetFileNameWithoutExtension(txtFilePath) + ".txt");
            // 重新載入訊息和索引
            LoadMessages();
            LoadLastIndex();
            UpdateMessage();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = 0;
            this.Top = SystemParameters.WorkArea.Height - this.ActualHeight;
            this.Width = SystemParameters.WorkArea.Width;
            maxTextWidth = this.ActualWidth - PrevButton.Width - NextButton.Width - 10;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveLastIndex();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Left)
                {
                    PrevButton_Click(sender, e);
                }
                else if (e.Key == Key.Right)
                {
                    NextButton_Click(sender, e);
                }
                else if (e.Key == Key.C)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else if (e.Key == Key.OemPlus)
                {
                    this.FontSize += 1;
                }
                else if (e.Key == Key.OemMinus)
                {
                    this.FontSize -= 1;
                }
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (messages.Any(m => !string.IsNullOrWhiteSpace(m)))
            {
                do
                {
                    currentMessageIndex = (currentMessageIndex - 1 + messages.Count) % messages.Count;
                } while (string.IsNullOrWhiteSpace(messages[currentMessageIndex]) && messages.Any(m => !string.IsNullOrWhiteSpace(m)));
                UpdateMessage();
                SaveLastIndex();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (messages.Any(m => !string.IsNullOrWhiteSpace(m)))
            {
                string currentMessage = messages[currentMessageIndex];
                if (!string.IsNullOrWhiteSpace(currentMessage))
                {
                    var formattedText = new FormattedText(
                        currentMessage,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(MessageTextBlock.FontFamily, MessageTextBlock.FontStyle, MessageTextBlock.FontWeight, MessageTextBlock.FontStretch),
                        MessageTextBlock.FontSize,
                        Brushes.White,
                        96.0);
                    if (formattedText.Width > maxTextWidth)
                    {
                        int charCount = EstimateVisibleCharacters(currentMessage, maxTextWidth);
                        string displayed = currentMessage.Substring(0, charCount);
                        string remaining = currentMessage.Substring(charCount);
                        messages.Insert(currentMessageIndex + 1, remaining);
                        messages[currentMessageIndex] = displayed;
                    }
                }
                do
                {
                    currentMessageIndex = (currentMessageIndex + 1) % messages.Count;
                } while (string.IsNullOrWhiteSpace(messages[currentMessageIndex]) && messages.Any(m => !string.IsNullOrWhiteSpace(m)));
                UpdateMessage();
                SaveLastIndex();
            }
        }

        private int EstimateVisibleCharacters(string text, double maxWidth)
        {
            int low = 0, high = text.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                var formattedText = new FormattedText(
                    text.Substring(0, mid),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(MessageTextBlock.FontFamily, MessageTextBlock.FontStyle, MessageTextBlock.FontWeight, MessageTextBlock.FontStretch),
                    MessageTextBlock.FontSize,
                    Brushes.White,
                    96.0);
                if (formattedText.Width <= maxWidth)
                    low = mid;
                else
                    high = mid - 1;
            }
            return low;
        }

        private void UpdateMessage()
        {
            var validMessages = messages.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
            if (validMessages.Any())
            {
                if (currentMessageIndex < 0 || currentMessageIndex >= messages.Count || string.IsNullOrWhiteSpace(messages[currentMessageIndex]))
                {
                    currentMessageIndex = 0;
                    while (currentMessageIndex < messages.Count && string.IsNullOrWhiteSpace(messages[currentMessageIndex]))
                    {
                        currentMessageIndex++;
                    }
                }
                MessageTextBlock.Text = messages[currentMessageIndex];
                if (this.Left == 0 && this.Top == SystemParameters.WorkArea.Height - this.ActualHeight)
                {
                    this.Top = SystemParameters.WorkArea.Height - this.ActualHeight;
                }
            }
            else
            {
                MessageTextBlock.Text = "無有效訊息";
            }
        }

        private void LoadMessages()
        {
            if (File.Exists(txtFilePath))
            {
                messages = new List<string>(File.ReadAllLines(txtFilePath));
            }
            else
            {
                messages = new List<string> { "TXT 檔案不存在，請創建 " + txtFilePath };
            }
        }

        private void LoadLastIndex()
        {
            if (File.Exists(indexFilePath))
            {
                string indexStr = File.ReadAllText(indexFilePath);
                if (int.TryParse(indexStr, out int index))
                {
                    currentMessageIndex = index;
                    if (currentMessageIndex < 0 || currentMessageIndex >= messages.Count)
                    {
                        currentMessageIndex = 0;
                    }
                }
            }
            else
            {
                currentMessageIndex = 0;
            }
        }

        private void SaveLastIndex()
        {
            try
            {
                File.WriteAllText(indexFilePath, currentMessageIndex.ToString());
                // 更新 config.json 中的索引
                var viewModel = (notifyIcon?.DataContext as NotifyIconViewModel);
                if (viewModel != null)
                {
                    var txtFiles = viewModel.GetType().GetField("txtFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(viewModel) as Dictionary<string, int>;
                    if (txtFiles != null && txtFiles.ContainsKey(txtFilePath))
                    {
                        txtFiles[txtFilePath] = currentMessageIndex;
                        string json = JsonSerializer.Serialize(txtFiles, new JsonSerializerOptions { WriteIndented = true });
                        Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
                        File.WriteAllText(configFilePath, json);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save index: {ex.Message}");
            }
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            maxTextWidth = this.ActualWidth - PrevButton.Width - NextButton.Width - 10;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveLastIndex();
            notifyIcon?.Dispose();
        }
    }
}