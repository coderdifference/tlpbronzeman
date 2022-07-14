using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TlpBronzeMan
{
    public class Backup
    {
        public class BackupFile
        {
            public int Operation { get; set; }
            public int Mission { get; set; }
            public int Turn { get; set; }
            public DateTime Created { get; set; }

            public static BackupFile FromGameSave(FileInfo file)
            {
                Regex regex = new("^save_LEGACY OP ([0-9]+)- Mission ([0-9]+), Turn ([0-9]+)$");
                var matches = regex.Match(file.Name);
                if (!matches.Success)
                {
                    throw new InvalidOperationException($"File {file.Name} cannot be parsed for OP, Mission, and Turn");
                }
                var groups = matches.Groups.Cast<Match>().First().Groups;
                return new BackupFile
                {
                    Operation = int.Parse(groups[1].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Mission = int.Parse(groups[2].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Turn = int.Parse(groups[3].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Created = file.LastWriteTime
                };
            }

            public string BackupPath => $@"Operation {Operation}\Mission {Mission}\Turn {Turn}-{Created:yyyyMMddHHmmss}.isb";
            public string SaveGameFileName => $"save_LEGACY OP {Operation}- Mission {Mission}, Turn {Turn}";

            public static BackupFile FromBackup(FileInfo file, string backupRoot)
            {
                if (!file.FullName.StartsWith($"{backupRoot}\\"))
                {
                    throw new InvalidOperationException($"File {file.Name} cannot be parsed for OP, Mission, and Turn");
                }
                Regex regex = new(@"Operation ([0-9]+)\\Mission ([0-9]+)\\Turn ([0-9]+)-([0-9]{14})[.]isb$");
                var matches = regex.Match(file.FullName[(backupRoot.Length + 1)..]);
                if (!matches.Success)
                {
                    throw new InvalidOperationException($"File {file.Name} cannot be parsed for OP, Mission, and Turn");
                }
                var groups = matches.Groups.Cast<Match>().First().Groups;
                return new BackupFile
                {
                    Operation = int.Parse(groups[1].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Mission = int.Parse(groups[2].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Turn = int.Parse(groups[3].Value, System.Globalization.NumberStyles.Integer, Thread.CurrentThread.CurrentCulture),
                    Created = DateTime.ParseExact(groups[4].Value, "yyyyMMddHHmmss", Thread.CurrentThread.CurrentCulture)
                };
            }

            public static BackupFile? FindBackup(string backupRoot, BackupFile saveFile)
            {
                var path = $@"Operation {saveFile.Operation}\Mission {saveFile.Mission}";
                var filter = $"Turn {saveFile.Turn}-.*";
                var file = Directory.EnumerateFiles(Path.Combine(backupRoot, path), filter).OrderByDescending(f => f).FirstOrDefault();
                if (file != null)
                {
                    return BackupFile.FromBackup(new FileInfo(file), backupRoot);
                }
                return null;
            }
        }
        public static Regex SavePattern => new(@"^save_LEGACY .*$");

        private static BackupFile? _last;
        public static BackupFile? LastUpdated { 
            get
            {
                return _last;
            }
            set 
            {
                _last = value;
                OnUpdate?.Invoke(_last);
            }
        }

        public static Action<BackupFile?>? OnUpdate { get; set; }

        public static void RestoreBackup(string restoreFile, string saveGameRoot, string backupRoot)
        {
            var backup = BackupFile.FromBackup(new FileInfo(restoreFile), backupRoot);
            try
            {
                var dir = new DirectoryInfo(saveGameRoot);

                foreach (var file in dir.EnumerateFiles("save_LEGACY*.*"))
                {
                    file.Delete();
                }

                string path = Path.Combine(saveGameRoot, backup.SaveGameFileName);
                File.Copy(restoreFile, path, true);
                new FileInfo(path).CreationTime = backup.Created;
                MessageBox.Show($"Restored backup of Operation {backup.Operation}, Mission {backup.Mission}, Turn {backup.Turn} made on {backup.Created:dd/MM/yyyy HH:mm:ss}", "Restore Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ioe)
            {
                MessageBox.Show($"Failed to restore backup file {restoreFile}\n\n{ioe.Message}\n\n{ioe.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ForceCreateBackup(string saveRoot, string backupRoot, int maxBackups)
        {
            //Grabs the most recently updated IronMan save in the save folder
            var saveDirectoryInfo = new DirectoryInfo(saveRoot);
            var files         = saveDirectoryInfo.GetFiles().OrderByDescending(x => x.LastAccessTime).ToList();
            var file          = files.FirstOrDefault(x => SavePattern.IsMatch(Path.GetFileName(x.FullName)));

            if (file != null)
            {
                var backup = BackupFile.FromGameSave(file);
                //Create our backup directory and file names
                var backupFileName = backup.BackupPath;
                var backupFullName = Path.Combine(backupRoot, backupFileName);

                try
                {
                    File.Copy(file.FullName, backupFullName, true);

                    //Only delete additional backups if the new backup was copied successfully
                    if (maxBackups > 0)
                    {
                        DeleteAdditionalBackups(new FileInfo(backupFullName).DirectoryName ?? backupRoot, maxBackups);
                    }
                    LastUpdated = backup;
                    MessageBox.Show($"Forced backup of Operation {backup.Operation}, Mission {backup.Mission}, Turn {backup.Turn} saved on {backup.Created:dd/MM/yyyy HH:mm:ss}", "Backup Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (IOException ioe)
                {
                    MessageBox.Show($"Failed to backup file {file.FullName}\n\n{ioe.Message}\n\n{ioe.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"No TLP Ironman saves found.", "No Ironman Saves", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static void DeleteAdditionalBackups(string saveFolder, int maxBackups)
        {
            var backupChildInfo = new DirectoryInfo(saveFolder);
            var backupFiles     = backupChildInfo.GetFiles().OrderBy(x => x.CreationTime).ToList();
            var numToDelete     = backupFiles.Count - maxBackups;
            if (numToDelete <= 0) return;
            foreach (var file in backupFiles.Take(numToDelete).ToList())
            {
                File.Delete(file.FullName);
            }
        }

        public static async void StartBackup(string saveRoot, string backupRoot, int maxBackups, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() => WaitForAndBackupNewFile(saveRoot, backupRoot, maxBackups, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private static void WaitForAndBackupNewFile(string saveRoot, string backupRoot, int maxBackups, CancellationToken token)
        {
            var saveGameDirectoryInfo = new DirectoryInfo(saveRoot);
            while (!token.IsCancellationRequested)
            {
                // get last update time for source folder
                var files = saveGameDirectoryInfo.GetFiles().Where(f => SavePattern.IsMatch(f.Name)).OrderByDescending(f => f.LastAccessTime).ToList();
                var newFile = files.FirstOrDefault();
                if (newFile != null)
                {
                    var backup = BackupFile.FromGameSave(newFile);
                    var backupPath = Path.Combine(backupRoot, backup.BackupPath);
                    if (!File.Exists(backupPath))
                    {
                        var path = new FileInfo(backupPath).DirectoryName;
                        if (path == null)
                        {
                            throw new ArgumentNullException(nameof(path));
                        }
                        Directory.CreateDirectory(path);
                        File.Copy(newFile.FullName, backupPath, true);
                        LastUpdated = backup;
                        if (maxBackups > 0)
                        {
                            DeleteAdditionalBackups(new FileInfo(backupPath).DirectoryName ?? backupRoot, maxBackups);
                        }
                    }
                }
                Thread.Sleep(10000);
            }
        }
    }
}
