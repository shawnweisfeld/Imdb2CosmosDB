using ImdbLoader2.Util;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class MyArgs
    {
        //See Instructions here:
        //https://github.com/adamabdelhamed/PowerArgs
        //http://www.nuget.org/packages/PowerArgs/

        [ArgShortcut("-c"), ArgDefaultValue(Command.ExecuteBatch)]
        public Command Command { get; set; }

        [ArgShortcut("-f")]
        public string File { get; set; }

        [ArgShortcut("-w"), ArgDefaultValue(true)]
        public bool WaitForUser { get; set; }


        public void Main()
        {
            MainAsync().Wait();

            if (WaitForUser)
            {
                ConsoleEx.WriteLineGreen("Done! (press any key to close)");
                Console.ReadKey();
            }
        }

        public async Task MainAsync()
        {
            try
            {
                switch (Command)
                {
                    case Command.DownloadImdbSource:
                        ConsoleEx.WriteLineGreen("Downloading IMDB Source");
                        await DownloadImdbSource.ExecuteAsync();
                        break;
                    case Command.ParseImdbSource:
                        ConsoleEx.WriteLineGreen("Parsing IMDB Source");
                        await ParseImdbSource.ExecuteAsync();
                        break;
                    case Command.ResetGraphDatabase:
                        ConsoleEx.WriteLineGreen($"Reset Graph Database");
                        await ResetGraphDatabase.ExecuteAsync();
                        break;
                    case Command.LoadImdbSource:
                        ConsoleEx.WriteLineGreen($"Loading IMDB Source - {File}");
                        await LoadImdbSource.ExecuteAsync(File);
                        break;
                    default:
                        ConsoleEx.WriteLineGreen("Batch Load IMDB Source");
                        await ExecuteBatch.ExecuteAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLineRed($"ERROR: {ex.ToString()}");
            }
        }
    }


    public enum Command
    {
        DownloadImdbSource,
        ParseImdbSource,
        ResetGraphDatabase,
        LoadImdbSource,
        ExecuteBatch,
    }
    
}
