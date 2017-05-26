using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public class BlobHelper
    {
        private CloudBlobClient BlobClient { get; set; }
        private CloudStorageAccount StorageAccount { get; set; }

        public BlobHelper(string storageConnectionString)
        {
            StorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            BlobClient = StorageAccount.CreateCloudBlobClient();
        }

        public async Task<CloudBlockBlob> UploadFileToContainerAsync(string containerName, string file)
        {
            using (var fs = File.OpenRead(file))
            {
                return await UploadFileToContainerAsync(containerName, Path.GetFileName(file), fs);
            }
        }

        public async Task<CloudBlockBlob> UploadFileToContainerAsync(string containerName, string blobName, Stream source)
        {
            var sw = new Stopwatch();
            sw.Start();
            ConsoleEx.WriteLineYellow($"Uploading stream [{blobName}] to container [{containerName}].");

            CloudBlobContainer container = BlobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromStreamAsync(source);

            sw.Stop();
            ConsoleEx.WriteLineGreen($"Upload stream [{blobName}] to container [{containerName}] completed in {sw.Elapsed.TotalSeconds:N2} seconds.");
            return blobData;
        }

        public async Task DownloadFileFromContainerAsync(string containerName, string blobName, Stream target)
        {
            ConsoleEx.WriteLineYellow($"Downloading stream [{blobName}] from container [{containerName}].");

            CloudBlobContainer container = BlobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.DownloadToStreamAsync(target);

            ConsoleEx.WriteLineGreen($"Download stream [{blobName}] from container [{containerName}] complete.");
        }

        public async Task DownloadFileFromContainerAsync(string containerName, string blobName, string target)
        {
            using (var fs = File.OpenWrite(target))
            {
                await DownloadFileFromContainerAsync(containerName, blobName, fs);
            }
        }

        public async Task UploadFileToContainerAsync(string containerName, string blobName, IEnumerable<string> lines)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(string.Join("\r\n", lines));
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await UploadFileToContainerAsync(containerName, blobName, stream);
            }
        }
        
        public async Task CreateContainerIfNotExistAsync(string containerName)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(containerName);
            var isComplete = false;

            do
            {
                try
                {
                    await container.CreateIfNotExistsAsync();
                    ConsoleEx.WriteLineGreen($"Container [{containerName}] created.");
                    isComplete = true;
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == 409)
                    {
                        ConsoleEx.WriteLineGreen($"Container [{containerName}] exists, will try creation again in 15 seconds.");
                        await Task.Delay(15000);
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (!isComplete);
        }

        public async Task DropConatinerIfExists(string containerName)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(containerName);

            if (await container.DeleteIfExistsAsync())
            {
                ConsoleEx.WriteLineGreen($"Container [{containerName}] deleted.");
            }
            else
            {
                ConsoleEx.WriteLineGreen($"Container [{containerName}] did not exist, skipping delete.");
            }
        }

        public IEnumerable<string> ListFilesInContainer(string containerName)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(containerName);
            return container.ListBlobs(prefix: null, useFlatBlobListing: true)
                .Select(x => (CloudBlob)x)
                .Select(x => x.Name);
        }
    }
}
