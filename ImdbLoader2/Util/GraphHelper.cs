using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public class GraphHelper
    {
        private DocumentClient Client { get; set; }
        private Database Database { get; set; }
        private DocumentCollection Graph { get; set; }


        private GraphHelper()
        { }

        public static async Task<GraphHelper> CreateAsync()
        {
            var me = new GraphHelper();
            
            me.Client = new DocumentClient(new Uri(Settings.GraphEndpoint), Settings.GraphKey,
                    new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });
            me.Database = await me.Client.CreateDatabaseIfNotExistsAsync(new Database { Id = Settings.GraphDatabase });

            Console.WriteLine("Creating collection if not exists.");
            me.Graph = new DocumentCollection { Id = Settings.GraphCollection };
            me.Graph.IndexingPolicy.IndexingMode = IndexingMode.Lazy;
            me.Graph.PartitionKey.Paths.Add("/pkey");
            me.Client.ConnectionPolicy.MaxConnectionLimit = Environment.ProcessorCount * Settings.ConnectionsPerProc;
            me.Graph = await me.Client.CreateDocumentCollectionIfNotExistsAsync(
                                                UriFactory.CreateDatabaseUri(Settings.GraphDatabase),
                                                me.Graph,
                                                new RequestOptions { OfferThroughput = Settings.GraphThroughput });

            return me;
        }

        public Task DropGraphAsync()
        {
            return Client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(Settings.GraphDatabase, Settings.GraphCollection)); 
        }
        

        public async Task<string> ExecuteGremlinQuery(string queryString)
        {
            Console.WriteLine($"Executing Query: {queryString}");
            var sb = new StringBuilder();
            var query = Client.CreateGremlinQuery<dynamic>(Graph, queryString);
            while (query.HasMoreResults)
            {
                foreach (dynamic result in await query.ExecuteNextAsync())
                {
                    sb.Append($"\t {JsonConvert.SerializeObject(result)}");
                }
            }
            Console.WriteLine($"Query Executed: {queryString}");
            return sb.ToString();
        }
    }
}
