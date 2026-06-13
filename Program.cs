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

            Console.WriteLine("Target drive: " + options.DriveLetter + ":");
            if (options.Verbose) Console.WriteLine("Verbose mode: enabled");
            if (!string.IsNullOrEmpty(options.LogFile)) Console.WriteLine("Log file: " + options.LogFile);
        }
    }
}
