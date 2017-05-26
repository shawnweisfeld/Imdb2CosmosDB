using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public class FtpHelper : IDisposable  
    {
        private FtpWebResponse response = null;

        public async Task<Stream> GetFile(string requestUriString, string user = "anonymous", string password = "foo")
        {
            var request = WebRequest.Create(requestUriString) as FtpWebRequest;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(user, password);

            response = (await request.GetResponseAsync()) as FtpWebResponse;

            return response.GetResponseStream();
        }

        public void Dispose()
        {
            if (response != null)
                response.Dispose();
        }
    }
}
