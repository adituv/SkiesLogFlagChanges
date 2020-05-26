using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace SkiesLogFlagChanges
{
    public static class Program
    {
        public class Options
        {
            [Option('o', "output", Required = false, HelpText = "Path to the file to output to.  Defaults to standard output (console).")]
            public string OutputPath { get; set; }

            [Option('l', "logpath", Required = false, HelpText = "Path to your dolphin.log file.  If this is not provided, the program attempts to locate it automatically.")]
            public string DolphinLogPath { get; set; }
        }

        private static bool IsRunning = true;

        private static void LogFlagChange(TextWriter writer, SoALFlagChangedEventArgs arg)
        {
            writer.WriteLine("Flag #{0:d3} changed: {1:x2} -> {2:x2}", arg.FlagNumber, arg.OldValue, arg.NewValue);
        }

        private static void LogBitChange(TextWriter writer, SoALBitChangedEventArgs arg)
        {
            writer.WriteLine("Bit #{0:d5} was {1}.\t\t(Chunk: {2}, Bit: {3})", arg.BitNumber, arg.IsBeingSet ? "set" : "cleared", arg.ChunkNumber, arg.ChunkBit);
        }

        public static void RunWithOptions(Options opts)
        {
            bool writeConsoleOut = true;
            string outputPath = null;
            string logPath = null;

            if (!string.IsNullOrEmpty(opts.DolphinLogPath))
            {
                logPath = opts.DolphinLogPath;
            }
            else
            {
                logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dolphin Emulator", "Logs", "dolphin.log");
            }

            outputPath = opts.OutputPath;

            TextWriter consoleWriter = null;
            TextWriter outputWriter = null;

            if (!string.IsNullOrEmpty(outputPath))
            {
                StreamWriter sw = new StreamWriter(outputPath)
                {
                    AutoFlush = true
                };
                outputWriter = sw;
            }

            if (writeConsoleOut)
            {
                consoleWriter = Console.Out;
            }

            // Console.CancelKeyPress += (s, e) => { IsRunning = false; };

            try
            {
                using (var logFile = new LogLineObservable(logPath))
                {
                    var flagObserver = new SoALFlagObserver();
                    var bitObserver = new SoALBitObserver();

                    if (outputWriter != null)
                    {
                        flagObserver.OnChange += (_, e) => LogFlagChange(outputWriter, e);
                        bitObserver.OnChange += (_, e) => LogBitChange(outputWriter, e);
                    }

                    if (consoleWriter != null)
                    {
                        flagObserver.OnChange += (_, e) => LogFlagChange(consoleWriter, e);
                        bitObserver.OnChange += (_, e) => LogBitChange(consoleWriter, e);
                    }

                    flagObserver.OnException += (_, e) =>
                    {
                        Console.Error.WriteLine("An unhandled exception occurred!");
                        Console.Error.WriteLine("\t{0}", ((Exception)e.ExceptionObject).Message);

                        IsRunning = false;
                    };
                    bitObserver.OnException += (_, e) =>
                    {
                        Console.Error.WriteLine("An unhandled exception occurred!");
                        Console.Error.WriteLine("\t{0}", ((Exception)e.ExceptionObject).Message);

                        IsRunning = false;
                    };

                    logFile.Subscribe(flagObserver);
                    logFile.Subscribe(bitObserver);
                    logFile.StartListening();
                    Console.Error.WriteLine("Listening to dolphin.log...");

                    if (!Console.IsInputRedirected)
                    {
                        Console.Error.WriteLine("[Press Q to exit]");
                        while (IsRunning && Console.ReadKey().Key != ConsoleKey.Q)
                        {
                            // Do nothing;
                        }
                    }
                    else
                    {
                        while (IsRunning)
                        {
                            // Do nothing;
                        }
                    }

                    logFile.StopListening();
                }
            }
            finally
            {
                consoleWriter?.Dispose();
                outputWriter?.Dispose();
            }
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(RunWithOptions).WithNotParsed((e) => Console.Error.WriteLine("Parse error"));
        }
    }
}
