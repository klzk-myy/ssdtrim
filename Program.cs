using System;
using System.IO;

namespace SsdTrim
{
    class TrimOptions
    {
        public string DriveLetter { get; set; }
        public bool Verbose { get; set; }
        public string LogFile { get; set; }
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

        static void Main(string[] args)
        {
            var options = ParseArgs(args);
            var logger = new Logger(options.Verbose, options.LogFile);

            if (string.IsNullOrEmpty(options.DriveLetter))
            {
                Console.WriteLine("SSD Trim Tool for Windows 7");
                Console.WriteLine("Usage: SsdTrim.exe -drive C: [-verbose] [-log path]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -drive   Drive letter (required, e.g. C)");
                Console.WriteLine("  -verbose Enable verbose output");
                Console.WriteLine("  -log     Log file path");
                return;
            }

            logger.Info("Starting SSD Trim operation");
            logger.Info("Target drive: " + options.DriveLetter + ":");
            logger.Verbose("Verbose mode enabled");
            logger.Verbose("Log file: " + (options.LogFile ?? "none"));
        }
    }
}
