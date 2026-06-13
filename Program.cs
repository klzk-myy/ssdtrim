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

    class Program
    {
        static TrimOptions ParseArgs(string[] args)
        {
            var options = new TrimOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-drive":
                        if (i + 1 < args.Length)
                            options.DriveLetter = args[++i].TrimEnd(':').ToUpper();
                        break;
                    case "-verbose":
                        options.Verbose = true;
                        break;
                    case "-log":
                        if (i + 1 < args.Length)
                            options.LogFile = args[++i];
                        break;
                }
            }
            return options;
        }

        static void Main(string[] args)
        {
            var options = ParseArgs(args);

            if (string.IsNullOrEmpty(options.DriveLetter))
            {
                Console.WriteLine("SSD Trim Tool for Windows 7");
                Console.WriteLine("Usage: SsdTrim.exe -drive C: [-verbose] [-log path]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -drive   Drive letter (required)");
                Console.WriteLine("  -verbose Enable verbose output");
                Console.WriteLine("  -log     Log file path");
                return;
            }

            Console.WriteLine("Target drive: " + options.DriveLetter + ":");
            if (options.Verbose) Console.WriteLine("Verbose mode: enabled");
            if (!string.IsNullOrEmpty(options.LogFile)) Console.WriteLine("Log file: " + options.LogFile);
        }
    }
}
