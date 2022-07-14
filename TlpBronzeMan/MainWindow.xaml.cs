using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using TlpBronzeMan.Properties;
using MessageBox = System.Windows.MessageBox;

namespace TlpBronzeMan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool Updating { get; set; }

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            Backup.OnUpdate += OnUpdate;
            Init();
        }

        private void OnUpdate(Backup.BackupFile? backup)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (backup == null)
                {
                    MostRecentBackup.Content = "No previous backup";
                }
                else
                {

                    MostRecentBackup.Content = $"Operation {backup.Operation}, Mission {backup.Mission}, Turn {backup.Turn} at {backup.Created:dd/MM/yyyy HH:mm:ss}";
                }
            });
        }

        public void UpdateSettings()
        {
            Settings.Default.BackupRootFolder = BackupTextbox.Text;
            Settings.Default.SaveGameFolder = SaveTextbox.Text;
            Settings.Default.MaxBackups = (int)MaxBackupSlider.Value;
            Settings.Default.LastUpdated = JsonSerializer.Serialize(Backup.LastUpdated);
            Settings.Default.Save();
        }

        public void LoadSettings()
        {
            BackupTextbox.Text = Settings.Default.BackupRootFolder;
            SaveTextbox.Text = Settings.Default.SaveGameFolder;
            MaxBackupSlider.Value = Settings.Default.MaxBackups;
            MaxBackupTextbox.Text = $"{Settings.Default.MaxBackups}";
        }
        public void Init()
        { 
            if (string.IsNullOrWhiteSpace(BackupTextbox.Text))
            {
                return;
            }
            Regex regex = new(@"^Operation [0-9]+\\Mission [0-9]+\\Turn [0-9]+-[0-9]{14}.isb$");
            var lastFile = Directory.EnumerateFiles(BackupTextbox.Text, "*", new EnumerationOptions { RecurseSubdirectories = true }).Where(f => regex.IsMatch(f.Substring(BackupTextbox.Text.Length + 1))).OrderByDescending(f => f).LastOrDefault();
            if (lastFile != null)
            {
                Backup.LastUpdated = Backup.BackupFile.FromBackup(new FileInfo(lastFile), BackupTextbox.Text);
            }
        }

        private void BackupKeepSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSettings();
        }

        private void ForceBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SaveTextbox.Text) && !string.IsNullOrEmpty(BackupTextbox.Text))
            {
                Backup.ForceCreateBackup(SaveTextbox.Text, BackupTextbox.Text, (int)MaxBackupSlider.Value);
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SaveTextbox.Text) && !string.IsNullOrEmpty(BackupTextbox.Text))
            {
                SaveTextbox.IsEnabled = false;
                BackupTextbox.IsEnabled = false;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                cancellationTokenSource.TryReset();
                Backup.StartBackup(SaveTextbox.Text, BackupTextbox.Text, (int)MaxBackupSlider.Value, cancellationTokenSource.Token);
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SaveTextbox.Text) && !string.IsNullOrEmpty(BackupTextbox.Text))
            {
                if (!string.IsNullOrWhiteSpace(RestoreBackupText.Text))
                {
                    Backup.RestoreBackup(RestoreBackupText.Text, SaveTextbox.Text, BackupTextbox.Text);
                }
                else
                {
                    MessageBox.Show("Please select a backup file to restore.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            SaveTextbox.IsEnabled = true;
            BackupTextbox.IsEnabled = true;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void RestoreBackupText_OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(SaveTextbox.Text) && !string.IsNullOrEmpty(BackupTextbox.Text))
            {
                string path = BackupTextbox.Text;
                if (Backup.LastUpdated != null)
                {
                    path = Directory.GetParent(Path.Combine(BackupTextbox.Text, Backup.LastUpdated.BackupPath))?.FullName ?? "";
                }
                Microsoft.Win32.OpenFileDialog dialog = new()
                {
                    DefaultExt = ".isb",
                    Filter = "Backup Files (*.isb)|*.isb",
                    InitialDirectory = path
                };

                var result = dialog.ShowDialog();
                if (result ?? false)
                {
                    RestoreBackupText.Text = dialog.FileName;
                }
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SaveTextbox.Text))
            {
                if (Directory.Exists(SaveTextbox.Text))
                {
                    System.Diagnostics.Process.Start(SaveTextbox.Text);
                }
                else
                {
                    MessageBox.Show($"Cannot open path {SaveTextbox.Text}. Directory does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(BackupTextbox.Text))
            {
                if (Directory.Exists(BackupTextbox.Text))
                {
                    System.Diagnostics.Process.Start(BackupTextbox.Text);
                }
                else
                {
                    MessageBox.Show($"Cannot open path {BackupTextbox.Text}. Directory does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBackupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(BackupTextbox.Text))
            {
                if (MessageBox.Show("This will delete all backups under the current backup root folder.\n\nAre you sure you want to proceed?", "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var backupList = Directory.GetFiles(BackupTextbox.Text, "*.isb", SearchOption.AllDirectories);
                    foreach (var backup in backupList)
                    {
                        try
                        {
                            File.Delete(backup);
                        }
                        catch (IOException ioe)
                        {
                            MessageBox.Show($"Failed to delete backup file {backup}\n\n{ioe.Message}\n\n{ioe.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                    }
                }
            }
            else
            {
                MessageBox.Show("Make sure that the save game folder and the backup folder have been set.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupTextbox_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.InitialDirectory = BackupTextbox.Text;
            dialog.Description = "Select Backup Root Folder";
            dialog.UseDescriptionForTitle = true;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupTextbox.Text = dialog.SelectedPath;
                UpdateSettings();
                Init();
            }
        }

        private void SaveTextbox_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.InitialDirectory = SaveTextbox.Text;
            dialog.Description = "Select TLP Save Game Folder";
            dialog.UseDescriptionForTitle = true;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveTextbox.Text = dialog.SelectedPath;
                UpdateSettings();
            }
        }
    }
}
