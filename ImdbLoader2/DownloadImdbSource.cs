using ImdbLoader2.Util;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImdbLoader2
{
    /// <summary>
    /// Download the source file from IMDB and cache it as-is in Azure blob storage.
    /// </summary>
    public static class DownloadImdbSource
    {
        public static async Task ExecuteAsync()
        {
            //Reiview the IMDB license for this data here
            //http://www.imdb.com/interfaces

            var bh = new BlobHelper(Settings.StorageAccountConnectionString);

            await bh.CreateContainerIfNotExistAsync(Settings.ImdbSourceFileContainer);

            if (bh.ListFilesInContainer(Settings.ImdbSourceFileContainer)
                .Contains(Path.GetFileName(new Uri(Settings.ImdbSourceFile).LocalPath)))
            {
                ConsoleEx.WriteLineYellow("already have file skipping");
            }
            else
            {
                using (var ftp = new FtpHelper())
                {
                    await bh.UploadFileToContainerAsync(Settings.ImdbSourceFileContainer,
                        Path.GetFileName(new Uri(Settings.ImdbSourceFile).LocalPath),
                        await ftp.GetFile(Settings.ImdbSourceFile));
                }
            }
        }

    }
}