using GenTools.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Scighost.WinUILib.Helpers;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using GenTools.Depend;
using System.IO.Compression;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GenTools
{
    public partial class MainWindow : Window
    {
        private GetNetData _getNetData;
        private readonly GetGiteeLatest _getGiteeLatest = new GetGiteeLatest();
        private readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();
        private static readonly HttpClient httpClient = new HttpClient();
        private IntPtr hwnd;
        string fileUrl;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;

        string backgroundUrl = "";

        private const string KeyPath = "GenTools";
        private const string ValueFirstRun = "Config_FirstRun";
        private const string ValueGamePath = "Config_GamePath";
        private const string ValueUnlockFPS = "Config_UnlockFPS";
        private const string ValueUpdateService = "Config_UpdateService";
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        private record LocalSettingsData(string FirstRun, string GamePath, string UnlockFPSValue, string UpdateService);

        public MainWindow()
        {
            
            Title = "原神工具箱";
            InitializeComponent();
            InitializeAppData();
            InitializeMicaBackground();
            InitializeWindowProperties();
            BackgroundImage(); 

            _getNetData = new GetNetData();
        }

        private void InitializeAppData()
        {
            //应用数据检查开始
            ApplicationDataContainer keyContainer = GetOrCreateContainer(KeyPath);
            LocalSettingsData defaultValues = new LocalSettingsData("1", "Null", "Null", "Null");
            LocalSettingsData currentValues = GetCurrentValues(keyContainer, defaultValues);
            if (currentValues.FirstRun == "1")
            {
                FirstRun.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
            }
            else if (currentValues.FirstRun == "0")
            {
                FirstRun.Visibility = Visibility.Collapsed;
                MainAPP.Visibility = Visibility.Visible;
            }
        }

        private void InitializeMicaBackground()
        {
            //设置背景Mica开始
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);
        }

        private void InitializeWindowProperties()
        {
            //设置窗口大小等开始
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);
            DisableWindowResize();

            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            Logging.Write("MoveAndResize to "+windowWidth+"*"+windowHeight,0);
            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;

                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
                Logging.Write("SetDragRectangles to "+48 * scale+"*"+ 48 * scale, 0);
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
        }

        private void DisableWindowResize()
        {
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);

            // Remove the WS_SIZEBOX style to disable resizing
            style &= ~NativeMethods.WS_SIZEBOX;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);
        }
        
        private async void BackgroundImage()
        {
            string apiUrl = "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;
            // 设置背景图片
            Logging.Write("Getting Background: " + backgroundUrl, 0);
            BitmapImage backgroundImage = new BitmapImage(new Uri(backgroundUrl));
            Background.ImageSource = backgroundImage;
        }

        //应用数据检查开始
        private ApplicationDataContainer GetOrCreateContainer(string keyPath)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (localSettings.Values["Config_TerminalMode"])
            {
                case 0:
                    TerminalMode.HideConsole();
                    break;
                case 1:
                    TerminalMode.ShowConsole();
                    break;
                default:
                    TerminalMode.HideConsole();
                    break;
            }
            if (localSettings.Containers.ContainsKey(keyPath))
            {
                return localSettings.Containers[keyPath];
            }
            else
            {
                return localSettings.CreateContainer(keyPath, ApplicationDataCreateDisposition.Always);
            }
        }

        // 获取当前设置值或默认值
        private LocalSettingsData GetCurrentValues(ApplicationDataContainer keyContainer, LocalSettingsData defaultValues)
        {
            string firstRunValue = GetValueOrDefault(keyContainer, ValueFirstRun, defaultValues.FirstRun);
            string gamePathValue = GetValueOrDefault(keyContainer, ValueGamePath, defaultValues.GamePath);
            string unlockFPSValue = GetValueOrDefault(keyContainer, ValueUnlockFPS, defaultValues.UnlockFPSValue);
            string updateService = GetValueOrDefault(keyContainer, ValueUpdateService, defaultValues.UpdateService);

            return new LocalSettingsData(firstRunValue, gamePathValue, unlockFPSValue, updateService);
        }

        // 获取键值对应的值，如果不存在则使用默认值
        private string GetValueOrDefault(ApplicationDataContainer keyContainer, string key, string defaultValue)
        {
            if (keyContainer.Values.ContainsKey(key))
            {
                return (string)keyContainer.Values[key];
            }
            else
            {
                return defaultValue;
            }
        }

        //选择下载渠道开始
        private void DSerivceChoose_Click(object sender, RoutedEventArgs e)
        {
            dservice_panel_left.Visibility = Visibility.Collapsed;
            dservice_panel_right.Visibility = Visibility.Collapsed;
            depend_panel_left.Visibility = Visibility.Visible;
            depend_panel_right.Visibility = Visibility.Visible;
            OnGetDependLatestReleaseInfo();
        }

        private void DService_Github_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 0;
            dserivce_finish.IsEnabled = true;
        }

        private void DService_Gitee_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 1;
            dserivce_finish.IsEnabled = true;
        }

        private void DService_JSG_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 2;
            dserivce_finish.IsEnabled = true;
        }

        //依赖下载开始

        private const string FileFolder = "\\JSG-LLC\\GenTools\\Depends";
        private const string ZipFileName = "GenToolsHelper.zip";
        private const string ExtractedFolder = "GenToolsHelper";

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + FileFolder, ZipFileName);
            Trace.WriteLine(fileUrl);
            ToggleDependGridVisibility(false);
            depend_Download.IsEnabled = false;
            var progress = new Progress<double>(DependReportProgress);
            bool downloadResult = false;
            try
            {
                downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
            }
            catch (Exception ex)
            {
                depend_Info.Text = ex.Message;
            }

            if (downloadResult)
            {
                string keyPath = "GenTools";
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                ApplicationDataContainer keyContainer = localSettings.Containers[keyPath];
                string extractionPath = Path.Combine(userDocumentsFolderPath + FileFolder, ExtractedFolder);
                if (File.Exists(extractionPath+ "\\GenTools_FPSUnlock.exe"))
                {
                    string valueFirstRun = "Config_FirstRun";
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[valueFirstRun] = "0";
                }
                else 
                {
                    string valueFirstRun = "Config_FirstRun";
                    Trace.WriteLine(userDocumentsFolderPath);
                    Trace.WriteLine(extractionPath);
                    ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[valueFirstRun] = "0";
                }
            }
            else
            {
                // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                ToggleDependGridVisibility(true, false);
            }
        }

        private void ToggleDependGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            depend_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            depend_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            depend_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
            depend_Btn_Icon.Glyph = downloadSuccess ? "&#xe73a;" : "&#xe8bb;";
        }

        private async void OnGetDependLatestReleaseInfo()
        {
            depend_Grid.Visibility = Visibility.Collapsed;
            depend_Progress_Grid.Visibility = Visibility.Visible;
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.gentoolshelper");
                switch (localSettings.Values["Config_UpdateService"])
                {
                    case 0:
                        //latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", "GenToolsHelper");
                        break;
                    case 1:
                        latestReleaseInfo = await _getGiteeLatest.GetLatestReleaseInfoAsync("JSG-JamXi", "GenToolsHelper");
                        break;
                    case 2:
                        latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.gentoolshelper");
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
                }

                fileUrl = latestReleaseInfo.DownloadUrl;
                dispatcher.TryEnqueue(() =>
                {
                    depend_Latest_Name.Text = $"依赖项: {latestReleaseInfo.Name}";
                    depend_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                    depend_Download.IsEnabled = true;
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                dispatcher.TryEnqueue(() =>
                {
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                    depend_Btn_Text.Text = "获取失败";
                    depend_Latest_Version.Text = ex.Message;
                    depend_Btn_Bar.Visibility = Visibility.Collapsed;
                    depend_Btn_Icon.Glyph = "&#xe8bb" ;
                });
            }
        }

        private void DependReportProgress(double progressPercentage)
        {
            depend_Btn_Bar.Value = progressPercentage;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 处理设置菜单项单击事件
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "home":
                        // 导航到主页
                        MainFrame.Navigate(typeof(MainView));
                        break;
                    case "startgame":
                        // 导航到启动游戏页
                        MainFrame.Navigate(typeof(StartGameView));
                        break;
                    case "gacha":
                        // 导航到启动游戏页
                        MainFrame.Navigate(typeof(GachaView));
                        break;
                }
            }
            if (args.IsSettingsSelected)
            {
                // 导航到默认设置页面
                MainFrame.Navigate(typeof(AboutView));
            }
        }
        public static async Task<ApiResponse> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseBody);
        }
        public class ApiResponse
        {
            public int retcode { get; set; }
            public string message { get; set; }
            public Data data { get; set; }
        }


        

    }
}