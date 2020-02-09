using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using GameBananaAPI;
using HedgeModManager;

namespace KakarotModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static string StartDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string AppPath = Path.Combine(StartDirectory, AppDomain.CurrentDomain.FriendlyName);
        public static string ProgramName = "KakarotModManager";
        public static string VersionString = $"{Version.Major}.{Version.Minor}-{Version.Revision}";
        public static string ConfigPath;
        public static string[] Args;
        public static Game CurrentGame = Games.Unknown;
        public static SteamGame CurrentSteamGame;
        public static List<SteamGame> SteamGames = null;
        public static bool Restart;

        public const string WebRequestUserAgent =
            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        public const string RepoOwner = "thesupersonic16";
        public const string RepoName = "kakarotmodmanager";


        [STAThread]
        public static void Main(string[] args)
        {
            // Language
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            // Use TLSv1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (args.Length > 2 && string.Compare(args[0], "-update", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    // The old pid gets passed in the CLI arguments and we use that to make sure the process is terminated before replacing it
                    int.TryParse(args[2], out int pid);
                    var process = Process.GetProcessById(pid);

                    process.WaitForExit();
                }
                catch { }

                File.Copy(AppPath, args[1], true);

                // Start a process that deletes our updater
                new Process()
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", $"/C choice /C Y /N /D Y /T 0 & Del \"{AppPath}\"")
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    }
                }.Start();

                Process.Start(args[1]);
                Environment.Exit(0);
                return;
            }

            var application = new App();
            application.InitializeComponent();
            application.ShutdownMode = ShutdownMode.OnMainWindowClose;
            application.MainWindow = new MainWindow();
            Args = args;

#if !DEBUG
            // Enable our Crash Window if Compiled in Release
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                ExceptionWindow.UnhandledExceptionEventHandler(e.ExceptionObject as Exception, e.IsTerminating);
            };
#endif
            Steam.Init();
            InstallGBHandlers();
            SteamGames = Steam.SearchForGames();
            if (SteamGames.Count != 0)
            {
                SelectSteamGame(SteamGames.First());
            }
            else
            {
                SelectSteamGame(new SteamGame("Dragon Ball Z Kakarot", Path.Combine(StartDirectory, "AT.exe"), "851850"));
            }

            if (CurrentSteamGame == null)
            {
                var dialog = new HedgeMessageBox($"No Games Found!", 
                    "Please make sure your games are properly installed on Steam or\nRun KakarotModManager inside of any of the supported game's directory.");

                dialog.AddButton("Exit", () =>
                {
                    Environment.Exit(0);
                });

                dialog.ShowDialog();
            }

            if (args.Length > 1 && args[0] == "-gb")
            {
                GBAPI.ParseCommandLine(args[1]);
                return;
            }

            do
            {
                Restart = false;
                application.Run(application.MainWindow);
            }
            while (Restart);
        }

        /// <summary>
        /// Sets the Current Game to the passed Steam Game
        /// </summary>
        /// <param name="steamGame">Steam Game to select</param>
        public static void SelectSteamGame(SteamGame steamGame)
        {
            if (steamGame == null)
                return;

            foreach (var game in Games.GetSupportedGames())
            {
                if (game.AppID == steamGame.GameID)
                {
                    CurrentGame = game;
                    CurrentSteamGame = steamGame;
                    StartDirectory = steamGame.RootDirectory;
                    RegistryConfig.LastGameDirectory = StartDirectory;
                    RegistryConfig.Save();
                }
            }
        }

        public static void InstallGBHandlers()
        {
            foreach (var game in Games.GetSupportedGames())
            {
                GBAPI.InstallGBHandler(game);
            }
        }

        /// <summary>
        /// Finds and returns an instance of SteamGame from a Game
        /// </summary>
        /// <param name="game">Game</param>
        /// <returns>Steam Game</returns>
        public static SteamGame GetSteamGame(Game game)
        {
            return SteamGames.FirstOrDefault(t => t.GameName == game.GameName);
        }

        public static (bool, HedgeModManager.Github.ReleaseInfo) CheckForUpdates()
        {
            var info = HedgeModManager.Github.GithubAPI.GetLatestRelease(RepoOwner, RepoName);
            var version = info == null ? Version : info.GetVersion();
            bool hasUpdate = version.Major >= Version.Major && (version.Minor > Version.Minor || version.Revision > Version.Revision);

            return (hasUpdate, info);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            window.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            window.Close();
        }

        private void MaxBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            window.WindowState = window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            var minbtn = (Button)window.Template.FindName("MinBtn", window);
            var maxbtn = (Button)window.Template.FindName("MaxBtn", window);
            maxbtn.IsEnabled = window.ResizeMode == ResizeMode.CanResizeWithGrip || window.ResizeMode == ResizeMode.CanResize;
            minbtn.IsEnabled = window.ResizeMode != ResizeMode.NoResize;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count < 1 || !(e.RemovedItems[0] is FrameworkElement))
                return;

            var oldControl = (FrameworkElement)((TabItem)e.RemovedItems[0]).Content;
            var control = (TabControl)sender;
            var tempArea = (System.Windows.Shapes.Shape)control.Template.FindName("PART_TempArea", (FrameworkElement)sender);
            var presenter = (ContentPresenter)control.Template.FindName("PART_Presenter", (FrameworkElement)sender);
            var target = new RenderTargetBitmap((int)control.ActualWidth, (int)control.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(oldControl);
            tempArea.HorizontalAlignment = HorizontalAlignment.Stretch;
            tempArea.Fill = new ImageBrush(target);
            tempArea.RenderTransform = new TranslateTransform();
            presenter.RenderTransform = new TranslateTransform();
            presenter.RenderTransform.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(control.ActualWidth, 0));
            tempArea.RenderTransform.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(0, -control.ActualWidth, (x, y) => { tempArea.HorizontalAlignment = HorizontalAlignment.Left; }));
            tempArea.Fill.BeginAnimation(Brush.OpacityProperty, CreateAnimation(1, 0));


            AnimationTimeline CreateAnimation(double from, double to,
                          EventHandler whenDone = null)
            {
                IEasingFunction ease = new BackEase
                { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
                var duration = new Duration(TimeSpan.FromSeconds(0.4));
                var anim = new DoubleAnimation(from, to, duration)
                { EasingFunction = ease };
                if (whenDone != null)
                    anim.Completed += whenDone;
                anim.Freeze();
                return anim;
            }
        }
    }
}
