using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using HedgeModManager;
using KakarotModManager;
using InputType = HedgeModManager.InputType;

namespace KakarotModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; set; }

        protected Timer StatusTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void RefreshModList()
        {
            ViewModel.ModsDB.Scan();
        }

        public void UpdateStatus(string str)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLbl.Content = str;
            });
            StatusTimer.Change(4000, Timeout.Infinite);
        }

        public void CheckForManagerUpdates()
        {
            UpdateStatus("Checking for updates");
            try
            {
                var update = App.CheckForUpdates();

                if (!update.Item1)
                {
                    UpdateStatus("No updates found");
                    return;
                }

                Dispatcher.Invoke(() =>
                {

                    // http://wasteaguid.info/
                    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.exe");

                    var info = update.Item2;
                    var dialog = new HedgeMessageBox(info.Name, info.Body, HorizontalAlignment.Right, TextAlignment.Left, InputType.MarkDown);

                    dialog.AddButton("Update", () =>
                    {
                        if (info.Assets.Count > 0)
                        {
                            var asset = info.Assets[0];
                            dialog.Close();
                            var downloader = new DownloadWindow($"Downloading KakarotModManager ({info.TagName})", asset.BrowserDownloadUrl.ToString(), path)
                            {
                                DownloadCompleted = () =>
                                {
                                    Process.Start(path, $"-update \"{App.AppPath}\" {Process.GetCurrentProcess().Id}");
                                    Application.Current.Shutdown();
                                }
                            };

                            downloader.Start();
                        }
                    });

                    dialog.ShowDialog();
                });

                UpdateStatus(string.Empty);
            }
            catch
            {
                UpdateStatus("Failed to check for updates");
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = ViewModel = new MainWindowViewModel();
            StatusTimer = new Timer((state) => UpdateStatus(string.Empty));
            Title = $"{App.ProgramName} ({App.VersionString}) - {App.CurrentGame.GameName}";
            RefreshModList();
            new Thread(() =>
            {
                CheckForManagerUpdates();
            }).Start();
        }

        private void SaveAndPlayButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ModsDB.Save();
            App.CurrentSteamGame.StartGame();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ModsDB.Save();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ModsDB.Scan();
        }

        private void OpenModsButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ModsDB.GetModsDirectory());
        }

        private void OpenGitHubProjectButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"https://github.com/{App.RepoOwner}/{App.RepoName}");
        }

        private void ModsList_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            {
                foreach (string path in paths)
                {
                    if (path.EndsWith(".pak"))
                    {
                        ModsDB.InstallMod(path);
                    }

                    if (path.EndsWith(".zip") || path.EndsWith(".7z") || path.EndsWith(".rar"))
                    {
                        ModsDB.InstallKMMMod(path);
                    }
                }
            }
            RefreshModList();
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            var mod = ModsList.SelectedValue as Mod;
            if (mod == null)
                return;
            new AboutModWindow(mod).ShowDialog();
        }

        private void DescMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var mod = ModsList.SelectedValue as Mod;
            if (mod == null)
                return;
            new AboutModWindow(mod).ShowDialog();
        }

        private void OCMFMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var mod = ModsList.SelectedValue as Mod;
            if (mod == null)
                return;
            Process.Start(mod.RootDirectory);
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var mod = ModsList.SelectedValue as Mod;
            if (mod == null)
                return;
            string title = mod.Title;

            var box = new HedgeMessageBox("WARNING", $"Are you sure you want to delete \"{title}\"?\nThis action can not be undone!");

            box.AddButton("  Cancel  ", () =>
            {
                box.Close();
            });

            box.AddButton("  Delete  ", () =>
            {
                ViewModel.ModsDB.DeleteMod(ModsList.SelectedItem as Mod);
                UpdateStatus($"Deleted {title}");
                RefreshModList();
                box.Close();
            });

            box.ShowDialog();
        }
    }
}
