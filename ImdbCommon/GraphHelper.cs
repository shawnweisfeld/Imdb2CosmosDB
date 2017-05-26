using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbCommon
{
    public class GraphHelper
    {
        private DocumentClient Client { get; set; }
        private Database Database { get; set; }
        private DocumentCollection Graph { get; set; }

        private string GraphEndpoint { get; set; }

        public static object CreateAsync(object graphEndpoint, object graphKey, object graphDatabase, object graphCollection, object graphThroughput, object connectionsPerProc)
        {
            throw new NotImplementedException();
        }

        private string GraphKey { get; set; }
        private string GraphDatabase { get; set; }
        private string GraphCollection { get; set; }
        private int GraphThroughput { get; set; }
        private int ConnectionsPerProc { get; set; }


        private GraphHelper(string graphEndpoint, string graphKey, string graphDatabase, string graphCollection, int graphThroughput, int connectionsPerProc)
        {
            this.GraphEndpoint = graphEndpoint;
            this.GraphKey = graphKey;
            this.GraphDatabase = graphDatabase;
            this.GraphCollection = graphCollection;
            this.GraphThroughput = graphThroughput;
            this.ConnectionsPerProc = connectionsPerProc;
        }

        public static async Task<GraphHelper> CreateAsync(string graphEndpoint, string graphKey, string graphDatabase, string graphCollection, int graphThroughput, int connectionsPerProc)
        {
            var me = new GraphHelper(graphEndpoint, graphKey, graphDatabase, graphCollection, graphThroughput, connectionsPerProc);
            
            me.Client = new DocumentClient(new Uri(me.GraphEndpoint), me.GraphKey,
                    new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });
            me.Database = await me.Client.CreateDatabaseIfNotExistsAsync(new Database { Id = me.GraphDatabase });

            Console.WriteLine("Creating collection if not exists.");
            me.Graph = new DocumentCollection { Id = me.GraphCollection };
            me.Graph.IndexingPolicy.IndexingMode = IndexingMode.Lazy;
            me.Graph.PartitionKey.Paths.Add("/pkey");
            me.Client.ConnectionPolicy.MaxConnectionLimit = Environment.ProcessorCount * me.ConnectionsPerProc;
            me.Graph = await me.Client.CreateDocumentCollectionIfNotExistsAsync(
                                                UriFactory.CreateDatabaseUri(me.GraphDatabase),
                                                me.Graph,
                                                new RequestOptions { OfferThroughput = me.GraphThroughput });

            return me;
        }

        public Task DropGraphAsync()
        {
            return Client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(this.GraphDatabase, this.GraphCollection)); 
        }
        

        public async Task<string> ExecuteGremlinQuery(string queryString, bool format = false)
        {
            Console.WriteLine($"Executing Query: {queryString}");
            var sb = new StringBuilder();
            var query = Client.CreateGremlinQuery<dynamic>(Graph, queryString);
            while (query.HasMoreResults)
            {
                foreach (dynamic result in await query.ExecuteNextAsync())
                {
                    if (format)
                    {
                        sb.Append(JsonConvert.SerializeObject(result, Formatting.Indented));
                    }
                    else
                    {
                        sb.Append($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }
            }
            Console.WriteLine($"Query Executed: {queryString}");
            return sb.ToString();
        }

        public async Task<List<T>> ExecuteGremlinQuery<T>(string queryString)
        {
            var result = new List<T>();
            var query = Client.CreateGremlinQuery<T>(Graph, queryString);
            while (query.HasMoreResults)
            {
                foreach (var foo in await query.ExecuteNextAsync<T>())
                {
                    result.Add(foo);
                }
            }

            return result;
        }
    }
}
