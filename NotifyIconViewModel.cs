using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace NoBorderWPF;

/// <summary>
/// Provides bindable properties and commands for the NotifyIcon. In this sample, the
/// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
/// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
/// </summary>

public partial class NotifyIconViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowWindowCommand))]
    public bool canExecuteShowWindow = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HideWindowCommand))]
    public bool canExecuteHideWindow;

    // 儲存 TXT 檔案路徑及其索引
    private Dictionary<string, int> txtFiles = new Dictionary<string, int>();
    private readonly string configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NoBorderWPF",
        "config.json");

    public NotifyIconViewModel()
    {
        LoadTxtFiles();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteShowWindow))]
    public void ShowWindow()
    {
        Application.Current.MainWindow ??= new MainWindow();
        Application.Current.MainWindow.Show(disableEfficiencyMode: true);
        CanExecuteShowWindow = false;
        CanExecuteHideWindow = true;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteHideWindow))]
    public void HideWindow()
    {
        Application.Current.MainWindow?.Hide(enableEfficiencyMode: true);
        CanExecuteShowWindow = true;
        CanExecuteHideWindow = false;
    }

    [RelayCommand]
    public void ExitApplication()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    public void AddTxtFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select a TXT file"
        };
        if (dialog.ShowDialog() == true)
        {
            string filePath = dialog.FileName;
            if (!txtFiles.ContainsKey(filePath))
            {
                txtFiles.Add(filePath, 0); // 初始化索引為 0
                SaveTxtFiles();
                // 通知 MainWindow 更新訊息來源
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.SwitchTxtSource(filePath);
                }
            }
        }
    }

    [RelayCommand]
    public void SwitchTxtSource(string filePath)
    {
        if (txtFiles.ContainsKey(filePath))
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SwitchTxtSource(filePath);
            }
        }
    }

    public IEnumerable<string> GetTxtFiles()
    {
        return txtFiles.Keys;
    }

    private void LoadTxtFiles()
    {
        try
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                txtFiles = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }
    }

    private void SaveTxtFiles()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
            string json = JsonSerializer.Serialize(txtFiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }
}