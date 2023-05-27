using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using GenTools.Depend;
using Spectre.Console;
using Microsoft.Win32;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using GenTools.Views.GachaViews;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace GenTools.Views
{
    public sealed partial class StartGameView : Page
    {
        private DispatcherQueueTimer dispatcherTimer_Launcher;
        private DispatcherQueueTimer dispatcherTimer_Game;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public StartGameView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to StartGameView",0);
            // 获取UI线程的DispatcherQueue
            var dispatcherQueue_Launcher = DispatcherQueue.GetForCurrentThread();
            var dispatcherQueue_Game = DispatcherQueue.GetForCurrentThread();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer_Launcher = dispatcherQueue_Launcher.CreateTimer();
            dispatcherTimer_Launcher.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer_Launcher.Tick += CheckProcess_Launcher;
            dispatcherTimer_Launcher.Start();
            dispatcherTimer_Game = dispatcherQueue_Game.CreateTimer();
            dispatcherTimer_Game.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer_Game.Tick += CheckProcess_Game;
            dispatcherTimer_Game.Start();

            if (localSettings.Values.ContainsKey("Config_GamePath"))
            {
                var value = localSettings.Values["Config_GamePath"] as string;
                Logging.Write("GamePath: "+ value,0);
                if (!string.IsNullOrEmpty(value) && value.Contains("Null"))
                {
                    UpdateUIElementsVisibility(0);
                }
                else
                {
                    UpdateUIElementsVisibility(1);
                }
            }
            else
            {
                UpdateUIElementsVisibility(0);
            }

            if (localSettings.Values.ContainsKey("Config_UnlockFPS"))
            {
                var value = localSettings.Values["Config_UnlockFPS"] as string;
                if (value == "1")
                {
                    unlockFPS.IsChecked = true;
                }
                else if (value == "0")
                {
                    unlockFPS.IsChecked = false;
                }
            }
            else
            {
                unlockFPS.IsChecked = false;
            }

            if (localSettings.Values.ContainsKey("Config_FPS_Config"))
            {
                if (localSettings.Values["Config_FPS_Config"] != null) 
                { 
                    var value = (double)localSettings.Values["Config_FPS_Config"];
                    FPS_Config.Value = value;
                }
            }
        }
        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            await AnsiConsole.Status().StartAsync("等待选择文件...", async ctx =>
            {
                var file = await picker.PickSingleFileAsync();
                if (file != null && file.Name == "YuanShen.exe")
                {
                    localSettings.Values["Config_GamePath"] = @file.Path;
                    UpdateUIElementsVisibility(1);
                }
                else
                {
                    ValidGameFile.Subtitle = "选择正确的YuanShen.exe\n通常位于[游戏根目录\\Genshin Impact Game\\YuanShen.exe]";
                    ValidGameFile.IsOpen = true;
                }
            });
        }

        public void RMGameLocation(object sender, RoutedEventArgs e)
        {
            localSettings.Values["Config_GamePath"] = @"Null";
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            if (unlockFPS.IsChecked ?? false)
            {
                var processInfo = new ProcessStartInfo(userDocumentsFolderPath + "\\JSG-LLC\\GenTools\\Depends\\GenToolsHelper\\GenTools_FPSUnlock.exe", "240 \"" +gamePath +"\"");

                //启动程序
                processInfo.UseShellExecute = true;        // 设置为 false，表示不使用 Shell 打开进程
                processInfo.CreateNoWindow = true;          // 设置为 true，表示不创建窗口
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;  // 设置窗口样式为隐藏
                processInfo.Verb = "runas";
                Process.Start(processInfo);
            }
            else
            {
                var processInfo = new ProcessStartInfo(userDocumentsFolderPath + "\\JSG-LLC\\GenTools\\Depends\\GenToolsHelper\\GenTools_FPSUnlock.exe", "60 \"" + gamePath + "\"");

                //启动程序
                processInfo.UseShellExecute = true;        // 设置为 false，表示不使用 Shell 打开进程
                processInfo.CreateNoWindow = true;          // 设置为 true，表示不创建窗口
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;  // 设置窗口样式为隐藏
                processInfo.Verb = "runas";
                Process.Start(processInfo);
            }
        }
        private void StartLauncher_Click(object sender, RoutedEventArgs e)
        {
            StartLauncher(null, null);
        }

        private void UnlockFPS_Click(object sender, RoutedEventArgs e) 
        {
            if (unlockFPS.IsChecked ?? false)
            {
                localSettings.Values["Config_UnlockFPS"] = "1";
            }
            else
            {
                localSettings.Values["Config_UnlockFPS"] = "0";
            }
        }

        private void UpdateUIElementsVisibility(int status)
        {
            if (status == 0) 
            {
                selectGame.IsEnabled = true;
                selectGame.Visibility = Visibility.Visible;
                rmGame.Visibility = Visibility.Collapsed;
                rmGame.IsEnabled = false;
                startGame.IsEnabled = false;
                startLauncher.IsEnabled = false;
            }
            else
            {
                selectGame.IsEnabled = false;
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                rmGame.IsEnabled = true;
                startGame.IsEnabled = true;
                startLauncher.IsEnabled = true;
            }
        }

        public void StartGame(TeachingTip sender, object args)
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath);
            
            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; 
            Process.Start(processInfo);
        }

        public void StartLauncher(TeachingTip sender, object args)
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath.Replace("YuanShen.exe", "..\\launcher.exe"));

            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
            Process.Start(processInfo);
        }

        // 定时器回调函数，检查进程是否正在运行
        private void CheckProcess_Game(DispatcherQueueTimer timer, object e)
        {
            if (Process.GetProcessesByName("YuanShen").Length > 0)
            {
                // 进程正在运行
                startGame.Visibility = Visibility.Collapsed;
                gameRunning.Visibility = Visibility.Visible;
            }
            else
            {
                // 进程未运行
                startGame.Visibility = Visibility.Visible;
                gameRunning.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckProcess_Launcher(DispatcherQueueTimer timer, object e)
        {
            if (Process.GetProcessesByName("launcher").Length > 0)
            {
                // 进程正在运行
                startLauncher.Visibility = Visibility.Collapsed;
                launcherRunning.Visibility = Visibility.Visible;
            }
            else
            {
                // 进程未运行
                startLauncher.Visibility = Visibility.Visible;
                launcherRunning.Visibility = Visibility.Collapsed;
            }
        }

        private void FPS_Config_Change(object sender, RangeBaseValueChangedEventArgs e)
        {
            double changeValue = FPS_Config.Value;
            if (changeValue != 60)
            {
                localSettings.Values["Config_FPS_Config"] = changeValue;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer_Game.Stop();
            dispatcherTimer_Launcher.Stop();
            Logging.Write("Timer Stopped", 0);
        }

    }
}
