using ImdbLoader2.Util;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImdbLoader2
{
    public class ExecuteBatch
    {
        public async static Task ExecuteAsync()
        {
            var bh = await BatchHelper.CreateAsync();

            await bh.CreatePoolIfNotExistAsync();
            await bh.CreateJobAsync(Settings.BatchJobId, Settings.BatchPoolId, true);

            await ResetGraphDatabase.ExecuteAsync();
            await DownloadImdbSource.ExecuteAsync();

            var files = await ParseImdbSource.ExecuteAsync();

            var nodes = files.Where(x => !x.StartsWith("Edges"))
                .Select(x => Path.GetFileNameWithoutExtension(x))
                .ToArray();
            
            foreach (var file in files.OrderBy(x => x.Substring(x.Length - 10, 6)))
            {
                var cmd = $"cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\ImdbLoader2.exe -c LoadImdbSource -f {file} -w false";

                if (file.StartsWith("Edges"))
                {
                    bh.AddTask(Path.GetFileNameWithoutExtension(file), cmd, nodes);
                }
                else
                {
                    bh.AddTask(Path.GetFileNameWithoutExtension(file), cmd);
                }
            }

            await bh.PublishTasksAsync(Settings.BatchJobId);

            ConsoleEx.WriteLineGreen("Job sent to Batch Service!");
        }
    }
}