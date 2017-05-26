using ImdbLoader2.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ImdbLoader2
{
    public class LoadImdbSource
    {
        public static async Task ExecuteAsync(string inputFile)
        {
            var bh = new BlobHelper(Settings.StorageAccountConnectionString);
            int loadedRecords = 0;
            int totalRecords = 0;
            var log = new List<string>();
            var sw = new Stopwatch();
            sw.Start();

            log.Add("------------------------------");
            log.Add("Node: " + Environment.GetEnvironmentVariable("AZ_BATCH_NODE_ID"));
            log.Add("Task: " + Environment.GetEnvironmentVariable("AZ_BATCH_TASK_ID"));
            log.Add("Job:  " + Environment.GetEnvironmentVariable("AZ_BATCH_JOB_ID"));
            log.Add("Pool: " + Environment.GetEnvironmentVariable("AZ_BATCH_POOL_ID"));
            log.Add("------------------------------");

            try
            {

                var gh = await GraphHelper.CreateAsync();

                await bh.CreateContainerIfNotExistAsync($"{Settings.ImdbParsedFileContainer}output");
                
                Console.WriteLine("Staring load.");

                var pBlock = new TransformBlock<string, List<string>>(async row =>
                {
                    var taskLog = new List<string>();
                    try
                    {
                        Console.WriteLine($"Loading Row {row}");
                        taskLog.Add($"{row}");
                        var parts = row.Split('|');

                        var query = string.Empty;
                        if (inputFile.StartsWith("Actor"))
                        {
                            query = $"g.addV('person').property('id', '{parts[0]}').property('pkey', '{parts[0]}').property('name', '{parts[1]}')";
                            taskLog.Add($"{await gh.ExecuteGremlinQuery(query)}");
                        }
                        else if (inputFile.StartsWith("Movie"))
                        {
                            query = $"g.addV('film').property('id', '{parts[0]}').property('pkey', '{parts[0]}').property('name', '{parts[1]}').property('released', '{parts[2]}')";
                            taskLog.Add($"{await gh.ExecuteGremlinQuery(query)}");
                        }
                        else if (inputFile.StartsWith("Edges"))
                        {
                            //              movie                              actor
                            query = $"g.V(['{parts[1]}','{parts[1]}']).addE('stars').to(g.V(['{parts[0]}','{parts[0]}']))";
                            taskLog.Add($"{await gh.ExecuteGremlinQuery(query)}");

                            ////              actor                              movie
                            //query = $"g.V('{parts[0]}').addE('acts').to(g.V('{parts[1]}'))";
                            //taskLog.Add($"{await gh.ExecuteGremlinQuery(query)}");
                        }
                        else
                        {
                            throw new Exception($"Unknown input file {inputFile}");
                        }

                        loadedRecords++;
                        Console.WriteLine($"Row Loaded {row}");
                    }
                    catch (Exception exCosmos)
                    {
                        Console.WriteLine($"ERROR: Loading Row {row}: {exCosmos}\r\n\r\n");
                        taskLog.Add($"ERROR:{exCosmos}\r\n\r\n");
                    }

                    return taskLog;
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * Settings.MaxDegreeOfParallelism
                });

                await bh.DownloadFileFromContainerAsync(Settings.ImdbParsedFileContainer, inputFile, inputFile);

                var content = File.ReadAllLines(inputFile);
                foreach (var row in content)
                {
                    totalRecords++;
                    pBlock.Post(row);
                }
                
                for (int i = 0; i < content.Length; i++)
                {
                    log.AddRange(pBlock.Receive());
                }

            }
            catch (Exception ex)
            {
                log.Add($"ERROR:{ex}");
                Console.WriteLine($"ERROR: Loading: {ex}");
            }

            sw.Stop();
            log.Add("------------------------------");
            log.Add($"Loaded {loadedRecords} of {totalRecords} at {loadedRecords / sw.Elapsed.TotalSeconds} records per second.");
            log.Add("------------------------------");

            //Upload the log to storage
            await bh.UploadFileToContainerAsync($"{Settings.ImdbParsedFileContainer}output", inputFile, log);
        }
        
    }
}