using ImdbLoader2.Model;
using ImdbLoader2.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace ImdbLoader2
{
    public static class ParseImdbSource
    {
        public static async Task<List<string>> ExecuteAsync()
        {
            var bh = new BlobHelper(Settings.StorageAccountConnectionString);
            var actorFileName = Path.GetFileNameWithoutExtension(new Uri(Settings.ImdbSourceFile).LocalPath);

            await bh.DropConatinerIfExists(Settings.ImdbParsedFileContainer);

            if (File.Exists(actorFileName))
            {
                ConsoleEx.WriteLineGreen("IMDB Actor File already here no need to re-download.");
            }
            else
            {
                using (var compressedStream = new MemoryStream())
                {
                    ConsoleEx.WriteLineGreen("Downloading into Memory from Blob Storage.");
                    await bh.DownloadFileFromContainerAsync(Settings.ImdbSourceFileContainer, Path.GetFileName(new Uri(Settings.ImdbSourceFile).LocalPath),
                        compressedStream);

                    compressedStream.Seek(0, SeekOrigin.Begin);

                    ConsoleEx.WriteLineGreen("Decompressing and writing to disk.");
                    using (FileStream decompressedFileStream = File.Create(actorFileName))
                    using (var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        decompressedStream.CopyTo(decompressedFileStream);
                    }
                }
            }

            ConsoleEx.WriteLineGreen($"Processing Actor File ({actorFileName})");
            await bh.CreateContainerIfNotExistAsync(Settings.ImdbParsedFileContainer);
            
            int fileID = 0;

            var sw = new Stopwatch();
            sw.Start();
            long rows = 0;

            long autonumber = 1;

            var actors = new Dictionary<string, Actor>();
            var movies = new Dictionary<string, Movie>();
            var edges = new Dictionary<string, Edge>();

            var rowQuery = ParseFile(actorFileName).AsQueryable();

            if (Settings.RowsToLoad > 0)
                rowQuery = rowQuery.Take(Settings.RowsToLoad);

            foreach (var row in rowQuery)
            {
                rows++;
                Actor actor = null;
                Movie movie = null;
                Edge edge = null;

                if (actors.ContainsKey(row.FullName))
                {
                    actor = actors[row.FullName];
                }
                else
                {
                    actor = new Actor()
                    {
                        Id = autonumber++,
                        FirstName = row.FirstName,
                        LastName = row.LastName
                    };
                    actors.Add(row.FullName, actor);
                }

                if (movies.ContainsKey(row.FullTitle))
                {
                    movie = movies[row.FullTitle];
                }
                else
                {
                    movie = new Movie()
                    {
                        Id = autonumber++,
                        Title = row.Title,
                        Year = row.Year
                    };
                    movies.Add(row.FullTitle, movie);
                }

                edge = new Edge()
                {
                    ActorId = actor.Id,
                    MovieId = movie.Id
                };

                if (!edges.ContainsKey(edge.FullName))
                {
                    edges.Add(edge.FullName, edge);
                }

                if (rows % Settings.FeedbackFrequency == 0)
                {
                    ConsoleEx.WriteLineGreen($"{row.RowNumb:n0}: {actor.FullName} ({actor.Id}) in {movie.FullTitle} ({movie.Id})");
                }
            }

            sw.Stop();

            
            ConsoleEx.WriteLineGreen($"Loaded {rows:n0} records in {sw.Elapsed.TotalSeconds:n0} seconds at {(rows / sw.Elapsed.TotalSeconds):n0} RPS.");
            ConsoleEx.WriteLineGreen($"Found {actors.Count():n0} Actors | {movies.Count():n0} Movies | {edges.Count():n0} Edges");
            List<string> fileNames = new List<string>();
            string fileName = string.Empty;
            Random rnd = new Random(); //shuffle the data, to eliminate hot partitions when loading into cosmos

            ConsoleEx.WriteLineGreen("Writing Actor Files");
            foreach (var item in actors.Select(x => x.Value).OrderBy(emp => rnd.Next()).ToBatch(Settings.BatchSize))
            {
                fileName = $"Actor-{fileID++:000000}.txt";
                await bh.UploadFileToContainerAsync(Settings.ImdbParsedFileContainer, fileName, item.Select(x => $"A{x.Id}|{x.FullName}"));
                fileNames.Add(fileName);
            }
            Console.WriteLine(" Done!");

            ConsoleEx.WriteLineGreen("Writing Movie Files");
            foreach (var item in movies.Select(x => x.Value).OrderBy(emp => rnd.Next()).ToBatch(Settings.BatchSize))
            {
                fileName = $"Movie-{fileID++:000000}.txt";
                await bh.UploadFileToContainerAsync(Settings.ImdbParsedFileContainer, fileName, item.Select(x => $"M{x.Id}|{x.Title}|{x.Year}"));
                fileNames.Add(fileName);
            }
            Console.WriteLine(" Done!");

            ConsoleEx.WriteLineGreen("Writing Edge Files");
            foreach (var item in edges.Select(x => x.Value).OrderBy(emp => rnd.Next()).ToBatch(Settings.BatchSize))
            {
                fileName = $"Edges-{fileID++:000000}.txt";
                await bh.UploadFileToContainerAsync(Settings.ImdbParsedFileContainer, fileName, item.Select(x => $"A{x.ActorId}|M{x.MovieId}"));
                fileNames.Add(fileName);
            }
            Console.WriteLine(" Done!");

            return fileNames;
        }

        private static IEnumerable<ActorMovieLine> ParseFile(string fileName)
        {
            long lineNumber = 0;
            bool isHeader = true;
            bool isFooter = false;
            string line;
            string lastLine = string.Empty;
            ActorMovieLine lastAM = null;

            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (isHeader)
                    {
                        if (lastLine.StartsWith("Name") && line.StartsWith("----"))
                        {
                            isHeader = false;
                        }
                    }
                    else if (line == "-----------------------------------------------------------------------------")
                    {
                        isFooter = true;
                    }
                    else if (!isHeader && !isFooter)
                    {
                        var row = new ActorMovieLine()
                        {
                            RowNumb = lineNumber
                        };

                        if (line.Length != 0)
                        {
                            var segments = line.Split('\t');


                            if (string.IsNullOrWhiteSpace(segments[0]))
                            {
                                //the name is empty use the name from the last record
                                row.FirstName = lastAM.FirstName;
                                row.LastName = lastAM.LastName;
                            }
                            else
                            {
                                //parse the name
                                string actor = segments[0];
                                int comma = actor.LastIndexOf(",");

                                if (comma > 0)
                                {
                                    row.FirstName = actor.Substring(comma + 1).Trim();
                                    row.LastName = actor.Substring(0, comma).Trim();
                                }
                                else
                                {
                                    row.FirstName = string.Empty;
                                    row.LastName = actor.Trim();
                                }
                            }

                            //year could be ???? or 2014/IV (year slash roman numeral)
                            string year = line.Find(@"\([0-9\?][0-9\?][0-9\?][0-9\?](/*)?(M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3}))?\)");

                            string title = line.Substring(0, line.IndexOf(year)).Trim();

                            if (title.StartsWith("\"") && title.EndsWith("\""))
                            {
                                title = title.TrimBoth();
                                row.IsTV = true;
                            }
                            else
                            {
                                row.IsTV = false;
                            }

                            row.Title = title.Trim();

                            var yearParts = year.TrimBoth().Split('/');

                            if (yearParts.Length == 2)
                            {
                                row.Year = yearParts[0];
                                //row.Roman = yearParts[1];
                            }
                            else
                            {
                                row.Year = year.TrimBoth();
                                //row.Roman = string.Empty;
                            }

                            //string characterName = Find(line, @"\[(.*?)\]");
                            //row.CharacterName = TrimEnds(characterName);

                            //string billingPosition = Find(line, @"\<(\d+)\>");
                            //row.BillingPosition = TrimEnds(billingPosition);

                            row.FirstName = row.FirstName.Clean().Trim();
                            row.LastName = row.LastName.Clean().Trim();
                            row.Title = row.Title.Clean().Trim();
                            row.Year = row.Year.Clean().Trim();

                            lastAM = row;

                            if (!row.IsTV)
                            {
                                yield return row;
                            }
                        }
                    }

                    lastLine = line;
                }
            }

        }
    }
}