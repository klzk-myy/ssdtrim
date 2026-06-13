using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace SsdTrim
{
    class TrimOptions
    {
        public string DriveLetter { get; set; }
        public bool Verbose { get; set; }
        public string LogFile { get; set; }
        public bool StatusOnly { get; set; }
    }

    class Logger
    {
        private readonly bool _verbose;
        private readonly string _logFile;
        private readonly object _lock = new object();

        public Logger(bool verbose, string logFile)
        {
            _verbose = verbose;
            _logFile = logFile;
        }

        public void Info(string message)
        {
            Log("INFO", message);
        }

        public void Verbose(string message)
        {
            if (_verbose) Log("DEBUG", message);
        }

        public void Error(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = "[" + timestamp + "] [ERROR] " + message;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logLine);
            Console.ResetColor();

            if (!string.IsNullOrEmpty(_logFile))
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFile, logLine + Environment.NewLine);
                }
            }
        }

        private void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = "[" + timestamp + "] [" + level + "] " + message;

            Console.WriteLine(logLine);

            if (!string.IsNullOrEmpty(_logFile))
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFile, logLine + Environment.NewLine);
                }
            }
        }
    }

    class Program
    {
        static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        static TrimOptions ParseArgs(string[] args)
        {
            var options = new TrimOptions();

            if (args == null) return options;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-drive":
                        if (i + 1 < args.Length)
                        {
                            string val = args[++i].TrimEnd(':').ToUpper();
                            if (val.Length == 1 && val[0] >= 'A' && val[0] <= 'Z')
                                options.DriveLetter = val;
                            else
                                Console.WriteLine("Warning: invalid drive letter '" + args[i] + "'");
                        }
                        else
                        {
                            Console.WriteLine("Warning: -drive requires a value (e.g. -drive C:)");
                        }
                        break;
                    case "-verbose":
                        options.Verbose = true;
                        break;
                    case "-status":
                        options.StatusOnly = true;
                        break;
                    case "-log":
                        if (i + 1 < args.Length)
                            options.LogFile = args[++i];
                        else
                            Console.WriteLine("Warning: -log requires a file path");
                        break;
                    default:
                        Console.WriteLine("Warning: unknown flag '" + args[i] + "'");
                        break;
                }
            }
            return options;
        }

        static bool PerformTrim(string driveLetter, Logger logger)
        {
            try
            {
                string rootPath = driveLetter + @":\";
                string tempFile = rootPath + "ssdtrim.tmp";

                DriveInfo drive = new DriveInfo(driveLetter);
                long freeBytes = drive.AvailableFreeSpace;

                logger.Info("Free space: " + FormatBytes(freeBytes));

                long margin = Math.Max(freeBytes / 100, 256L * 1024 * 1024);
                long trimSize = freeBytes - margin;

                if (trimSize <= 0)
                {
                    logger.Error("Not enough free space to perform trim (need at least 256 MB free)");
                    return false;
                }

                logger.Info("Trim size: " + FormatBytes(trimSize));
                logger.Verbose("Safety margin: " + FormatBytes(margin));

                const int bufferSize = 4 * 1024 * 1024;
                byte[] zeroBuffer = new byte[bufferSize];
                long totalWritten = 0;

                logger.Info("Writing zeros to " + tempFile + "...");
                Stopwatch sw = Stopwatch.StartNew();

                using (FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                {
                    while (totalWritten < trimSize)
                    {
                        long remaining = trimSize - totalWritten;
                        int writeSize = (int)Math.Min(bufferSize, remaining);
                        fs.Write(zeroBuffer, 0, writeSize);
                        totalWritten += writeSize;
                    }
                    fs.Flush(true);
                }

                sw.Stop();
                double seconds = sw.Elapsed.TotalSeconds;
                double speed = trimSize / (1024.0 * 1024.0) / seconds;
                logger.Info("Write completed in " + seconds.ToString("F1") + " seconds (" + speed.ToString("F0") + " MB/s)");

                logger.Info("Deleting temporary file to trigger TRIM...");
                sw = Stopwatch.StartNew();

                File.Delete(tempFile);

                sw.Stop();
                double deleteMs = sw.Elapsed.TotalMilliseconds;
                logger.Info("File deleted in " + deleteMs.ToString("F0") + " ms");

                logger.Info("TRIM hints sent for " + FormatBytes(trimSize) + " of free space");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("TRIM failed: " + ex.Message);
                return false;
            }
        }

        static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return size.ToString("F2") + " " + suffixes[order];
        }

        static bool CheckTrimStatus(Logger logger)
        {
            logger.Info("Checking TRIM status via fsutil...");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "fsutil",
                    Arguments = "behavior query DisableDeleteNotify",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    logger.Verbose("fsutil output: " + output.Trim());

                    if (output.Contains("DisableDeleteNotify = 0"))
                    {
                        logger.Info("TRIM is ENABLED (DisableDeleteNotify = 0)");
                        return true;
                    }
                    else if (output.Contains("DisableDeleteNotify = 1"))
                    {
                        logger.Info("TRIM is DISABLED (DisableDeleteNotify = 1)");
                        logger.Info("Enable with: fsutil behavior set DisableDeleteNotify 0");
                        return false;
                    }
                    else
                    {
                        logger.Info("TRIM status: not explicitly set (auto-enabled for SSDs)");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to query TRIM status: " + ex.Message);
                return false;
            }
        }

        static void Main(string[] args)
        {
            var options = ParseArgs(args);
            var logger = new Logger(options.Verbose, options.LogFile);

            if (options.StatusOnly)
            {
                CheckTrimStatus(logger);
                Environment.ExitCode = 0;
                return;
            }

            if (string.IsNullOrEmpty(options.DriveLetter))
            {
                Console.WriteLine("SSD Trim Tool for Windows 7");
                Console.WriteLine("Usage: SsdTrim.exe -drive C: [-verbose] [-log path]");
                Console.WriteLine("       SsdTrim.exe -status [-verbose] [-log path]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -drive   Drive letter (required for trim, e.g. C)");
                Console.WriteLine("  -status  Query TRIM status only");
                Console.WriteLine("  -verbose Enable verbose output");
                Console.WriteLine("  -log     Log file path");
                return;
            }

            logger.Info("Starting SSD Trim operation");
            logger.Info("Target drive: " + options.DriveLetter + ":");

            if (!IsAdministrator())
            {
                logger.Error("This program requires Administrator privileges");
                logger.Error("Please run as Administrator (right-click -> Run as Administrator)");
                Environment.ExitCode = 1;
                return;
            }
            logger.Verbose("Administrator privileges confirmed");

            if (!Directory.Exists(options.DriveLetter + @":\"))
            {
                logger.Error("Drive " + options.DriveLetter + ": does not exist");
                Environment.ExitCode = 1;
                return;
            }
            logger.Verbose("Drive " + options.DriveLetter + ": exists");

            bool success = PerformTrim(options.DriveLetter, logger);

            if (success)
            {
                logger.Info("Operation completed successfully");
                Environment.ExitCode = 0;
            }
            else
            {
                logger.Error("Operation failed");
                Environment.ExitCode = 1;
            }
        }
    }
}
