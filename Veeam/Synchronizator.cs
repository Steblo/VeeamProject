using log4net;
using System.Timers;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]

namespace Veeam
{
    public class Synchronizator
    {
        private static System.Timers.Timer aTimer;
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static int Interval { get; set; }

        public static string OriginFolder { get; set; }

        public static string SynchronizedFolder { get; set; }

        public static void Main(string[] args)
        {
            try
            {
                logger.Info("Synchronizator started.");

                if (args.Length != 3)
                {
                    logger.Error("Command line arguments not set.");
                    logger.Error("Veeam.exe OriginFolder SynchronizedFolder Interval");
                    return;
                }

                logger.Info("Validating 3 commandline arguments.");
                if (!ValidateFolder(args[0]))
                {
                    return;
                }
                ;
                if (!ValidateCreateFolder(args[1]))
                {
                    return;
                }
                if (!ValidateIntervalTime(args[2]))
                {
                    return;
                }
                logger.Info("Commandline arguments validated.");

                logger.Info("Setting Timer.");
                SetTimer(Interval);

                Console.WriteLine("\nPress the Enter key to exit the application...\n");
                Console.ReadLine();
                aTimer.Stop();
                aTimer.Dispose();

                logger.Info("Terminating the application...");
            }
            catch (Exception e)
            {
                logger.Error($"Error happened. {e}");
            }
        }

        private static bool ValidateFolder(string path)
        {
            if (Directory.Exists(path))
            {
                OriginFolder = path;
                logger.Info($"Folder validated. {path}");
                return true;
            }
            else
            {
                logger.Error($"Folder not found. {path}");
                return false;
            }
        }

        private static bool ValidateCreateFolder(string path)
        {
            SynchronizedFolder = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                logger.Info($"Folder created. {path}");
            }

            logger.Info($"Folder validated. {path}");
            return true;

        }

        private static bool ValidateIntervalTime(string time)
        {
            if (int.TryParse(time, out int miliseconds))
            {
                Interval = miliseconds;
                logger.Info("Synchronyzation interval validated.");
                return true;
            }
            else
            {
                logger.Error("Synchronyzation interval not validated.");
                return false;
            }
        }

        private static void SetTimer(int interval)
        {
            aTimer = new System.Timers.Timer(interval);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(object? sender, ElapsedEventArgs e)
        {
            try
            {
                Synchronize();
            }
            catch (Exception ex)
            {
                logger.Error($"Error happened. {ex}");
            }
        }

        private static void Synchronize()
        {
            logger.Info($"Synchronization started. {DateTime.Now}");
            logger.Info($"Processing files.");
            SynchronizeFiles(OriginFolder, SynchronizedFolder);
            logger.Info($"Processing subfolders.");
            SynchronizeSubFoldersRecursively(OriginFolder, SynchronizedFolder);
            logger.Info($"Synchronization complete. {DateTime.Now}");
        }

        private static void SynchronizeFiles(string sourceDir, string destinationDir)
        {
            var originFiles = Directory.GetFiles(sourceDir).OrderDescending().ToList();
            var synchronizedFiles = Directory.GetFiles(destinationDir).OrderDescending().ToList();

            if (originFiles.Count == 0)
            {
                logger.Info($"No Files found. {sourceDir}");
                var di = new DirectoryInfo(destinationDir);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                    logger.Info($"File deleted. {file}");
                }
                return;
            }

            if (synchronizedFiles.Count == 0)
            {
                logger.Info($"No Files found. {destinationDir}");
                var di = new DirectoryInfo(sourceDir);
                foreach (FileInfo file in di.GetFiles())
                {
                    File.Copy(Path.Combine(sourceDir, file.Name), Path.Combine(destinationDir, file.Name));
                    logger.Info($"File copied. {file.Name}");
                }
                return;
            }

            int originIndex = 0;
            int synchronizedIndex = 0;
            string? originFilePath = string.Empty;
            string? synchronizedFilePath = string.Empty;

            while (originIndex != originFiles.Count || synchronizedIndex != synchronizedFiles.Count)
            {
                if (originIndex < originFiles.Count)
                {
                    originFilePath = originFiles.ElementAt(originIndex);
                }

                if (synchronizedIndex < synchronizedFiles.Count)
                {
                    synchronizedFilePath = synchronizedFiles.ElementAt(synchronizedIndex);
                }

                var originFile = Path.GetFileName(originFilePath);
                var synchronizedFile = Path.GetFileName(synchronizedFilePath);

                if (originIndex != originFiles.Count)
                {
                    logger.Info($"Processing file. {originFile}");
                }
                else
                {
                    logger.Info($"Processing file. {synchronizedFiles}");
                }

                if (string.Compare(originFile, synchronizedFile) > 0)
                {
                    File.Delete(synchronizedFilePath);
                    synchronizedFiles.RemoveAt(synchronizedIndex);
                    logger.Info($"File deleted. {synchronizedFile}");
                    synchronizedIndex++;
                    continue;
                }

                if (string.Compare(originFile, synchronizedFile) < 0)
                {
                    File.Copy(originFilePath, Path.Combine(Path.GetDirectoryName(synchronizedFilePath), originFile));
                    logger.Info($"File copied. {originFile}");
                    originIndex++;
                    continue;
                }

                var originTime = File.GetLastWriteTime(originFilePath);
                var synchronizedTime = File.GetLastWriteTime(synchronizedFilePath);

                if (DateTime.Compare(originTime, synchronizedTime) != 0)
                {
                    File.Copy(Path.Combine(sourceDir, originFile), Path.Combine(destinationDir, originFile), true);
                    logger.Info($"File updated. {originFile}");
                }

                if (originIndex != originFiles.Count)
                {
                    originIndex++;
                }

                if (synchronizedIndex != synchronizedFiles.Count)
                {
                    synchronizedIndex++;
                }
            }
        }

        private static void SynchronizeSubFoldersRecursively(string sourceDir, string destinationDir)
        {
            var originDirs = Directory.GetDirectories(sourceDir).OrderDescending().ToList();
            var synchronizedDirs = Directory.GetDirectories(destinationDir).OrderDescending().ToList();

            if (originDirs.Count == 0)
            {
                logger.Info($"No Folders found. {sourceDir}");
                var di = new DirectoryInfo(destinationDir);
                foreach (var dir in di.GetDirectories())
                {
                    dir.Delete();
                    logger.Info($"Folder deleted. {dir}");
                }
                return;
            }

            if (synchronizedDirs.Count == 0)
            {
                logger.Info($"No Folders found. {destinationDir}");
                var di = new DirectoryInfo(sourceDir);
                foreach (var dir in di.GetDirectories())
                {
                    CopyDirectory(Path.Combine(sourceDir, dir.Name), Path.Combine(destinationDir, dir.Name));
                    logger.Info($"Folder copied. {dir.Name}");
                }
                return;
            }

            int originIndex = 0;
            int synchronizedIndex = 0;
            string? originDirPath = string.Empty;
            string? synchronizedDirPath = string.Empty;

            while (originIndex != originDirs.Count || synchronizedIndex != synchronizedDirs.Count())
            {
                if (originIndex < originDirs.Count)
                {
                    originDirPath = originDirs.ElementAt(originIndex);
                }

                if (synchronizedIndex < synchronizedDirs.Count)
                {
                    synchronizedDirPath = synchronizedDirs.ElementAt(synchronizedIndex);
                }

                var originInfo = new DirectoryInfo(originDirPath);
                var originDir = originInfo.Name;
                var synchronizedInfo = new DirectoryInfo(synchronizedDirPath);
                var synchronizedDir = synchronizedInfo.Name;

                if (originIndex != originDirs.Count)
                {
                    logger.Info($"Processing folder. {originDir}");
                }
                else
                {
                    logger.Info($"Processing folder. {synchronizedDir}");
                }

                if (string.Compare(originDir, synchronizedDir) > 0)
                {
                    Directory.Delete(synchronizedDirPath, true);
                    synchronizedDirs.RemoveAt(synchronizedIndex);
                    logger.Info($"Folder deleted. {synchronizedDir}");
                    synchronizedIndex++;
                    continue;
                }

                if (string.Compare(originDir, synchronizedDir) < 0)
                {
                    CopyDirectory(originDirPath, synchronizedDirPath);
                    logger.Info($"Folder copied. {originDir}");
                    originIndex++;
                    continue;
                }

                var originTime = File.GetLastWriteTime(originDirPath);
                var synchronizedTime = File.GetLastWriteTime(synchronizedDirPath);

                if (DateTime.Compare(originTime, synchronizedTime) != 0)
                {
                    SynchronizeFiles(originDirPath, Path.Combine(destinationDir, synchronizedDirPath));
                    SynchronizeSubFoldersRecursively(originDirPath, synchronizedDirPath);
                }

                if (originIndex != originDirs.Count)
                {
                    originIndex++;
                }

                if (synchronizedIndex != synchronizedDirs.Count)
                {
                    synchronizedIndex++;
                }
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
}
