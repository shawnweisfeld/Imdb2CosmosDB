using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public class BatchHelper : IDisposable
    {
        public BatchClient BatchClient { get; set; }
        public List<CloudTask> Tasks { get; set; }
        private BatchHelper()
        {

        }

        public static async Task<BatchHelper> CreateAsync()
        {
            var me = new BatchHelper();
            var cred = new BatchSharedKeyCredentials(Settings.BatchAccountUrl, Settings.BatchAccountName, Settings.BatchAccountKey);

            me.Tasks = new List<CloudTask>();
            me.BatchClient = await BatchClient.OpenAsync(cred);

            return me;
        }

        public async Task CreatePoolIfNotExistAsync()
        {
            var bh = new BlobHelper(Settings.StorageAccountConnectionString);
            var resourceFiles = new List<ResourceFile>();

            await bh.DropConatinerIfExists(Settings.BatchAppStorageContainer);
            await bh.CreateContainerIfNotExistAsync(Settings.BatchAppStorageContainer);


            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1),
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                Permissions = SharedAccessBlobPermissions.Read
            };

            foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory))
            {
                var blobData = await bh.UploadFileToContainerAsync(Settings.BatchAppStorageContainer, file);

                string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
                string blobSasUri = String.Format($"{blobData.Uri}{sasBlobToken}");

                resourceFiles.Add(new ResourceFile(blobSasUri, Path.GetFileName(file)));
            }


            CloudPool pool = null;
            try
            {
                Console.WriteLine("Creating pool [{0}]...", Settings.BatchPoolId);

                pool = BatchClient.PoolOperations.CreatePool(
                    poolId: Settings.BatchPoolId,
                    targetLowPriorityComputeNodes: Settings.ComputeNodes,
                    virtualMachineSize: Settings.VirtualMachineSize,  
                    cloudServiceConfiguration: new CloudServiceConfiguration(osFamily: "5"));   // Windows Server 2016
                pool.StartTask = new StartTask
                {
                    CommandLine = "cmd /c (robocopy %AZ_BATCH_TASK_WORKING_DIR% %AZ_BATCH_NODE_SHARED_DIR%) ^& IF %ERRORLEVEL% LEQ 1 exit 0",
                    ResourceFiles = resourceFiles,
                    WaitForSuccess = true
                };

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Swallow the specific error code PoolExists since that is expected if the pool already exists
                if (be.RequestInformation?.BatchError != null && be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", Settings.BatchPoolId);
                }
                else
                {
                    throw; // Any other exception is unexpected
                }
            }
        }

        public async Task CreateJobAsync(string jobId, string poolId, bool useTaskDependencies = false)
        {
            ConsoleEx.WriteLineGreen($"Creating job [{jobId}].");

            CloudJob job = BatchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };
            job.UsesTaskDependencies = useTaskDependencies;

            await job.CommitAsync();
        }

        public void AddTask(string taskId, string command, params string[] dependentTasks)
        {
            var task = new CloudTask(taskId, command);

            if (dependentTasks != null && dependentTasks.Any())
            {
                task.DependsOn = TaskDependencies.OnIds(dependentTasks);
            }

            Tasks.Add(task);
        }

        public async Task PublishTasksAsync(string jobId)
        {
            foreach (var taskSet in Tasks.ToBatch(250))
            {
                await BatchClient.JobOperations.AddTaskAsync(jobId, taskSet);
            }
        }

        public void Dispose()
        {
            if (BatchClient != null)
                BatchClient.Dispose();
        }
    }
}
