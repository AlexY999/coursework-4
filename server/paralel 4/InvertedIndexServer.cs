using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace paralel_4
{
    static class InvertedIndexServer
    {
        private const int Port = 8888;
        private const int Variant = 27;
        private static bool _isRunning = true;
        private static readonly ConcurrentDictionary<string, List<string>> InvertedIndex = new ConcurrentDictionary<string, List<string>>();

        public static void StartServer()
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            listener.Start();
            Console.WriteLine("Server is listening on port {0}...", Port);

            //BuildInvertedIndexForVariant(Variant, maxDegreeOfParallelism: Environment.ProcessorCount);
            for (int i = 1; i < 20; i = i + 1) {
                BuildInvertedIndexForVariant(Variant, maxDegreeOfParallelism: i);
            }
            BuildInvertedIndexForVariant(Variant, maxDegreeOfParallelism: Environment.ProcessorCount);

            Task.Run(() => AcceptClients(listener));

            Console.WriteLine("Press any key to stop the server.");
            Console.ReadKey();

            _isRunning = false;
            listener.Stop();
            Console.WriteLine("Server stopped.");
        }

        static void AcceptClients(TcpListener listener)
        {
            while (_isRunning)
            {
                var client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected from {0}", client.Client.RemoteEndPoint);
                ThreadPool.QueueUserWorkItem(_ => HandleClientRequest(client));
            }
        }

        static void HandleClientRequest(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    while (true)
                    {
                        string keyword = reader.ReadLine();

                        if (string.IsNullOrEmpty(keyword) || keyword.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        var foundDocuments = SearchInInvertedIndex(keyword);
                        string response = foundDocuments.Count > 0
                            ? $"Found in documents: {string.Join(", ", foundDocuments)}"
                            : "No documents found.";
                        writer.WriteLine(response);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }


        static List<string> SearchInInvertedIndex(string keyword)
        {
            if (InvertedIndex.TryGetValue(keyword, out List<string> documents))
            {
                return documents;
            }
            return new List<string>();
        }

        static void BuildInvertedIndexForVariant(int variant, int maxDegreeOfParallelism)
        {
            var filePaths = FileHelper.GetFilePathsForVariant(variant);
            var chunks = ChunkFileList(filePaths, maxDegreeOfParallelism);

            var tasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();

            foreach (var chunk in chunks)
            {
                tasks.Add(Task.Run(() =>
                {
                    foreach (var filePath in chunk)
                    {
                        var content = File.ReadAllText(filePath);
                        var documentId = Path.GetFileName(filePath);
                        AddDocumentToIndex(documentId, content);
                    }
                }));
            }

            Task.WhenAll(tasks).ContinueWith(t =>
            {
                stopwatch.Stop();
                LogPerformanceData(maxDegreeOfParallelism, stopwatch.ElapsedMilliseconds);
                WriteDictionaryToFile();
            }).Wait();
        }

        static List<List<string>> ChunkFileList(List<string> filePaths, int chunks)
        {
            var chunkedFiles = new List<List<string>>();
            int chunkSize = (int)Math.Ceiling((double)filePaths.Count / chunks);

            for (int i = 0; i < filePaths.Count; i += chunkSize)
            {
                chunkedFiles.Add(filePaths.GetRange(i, Math.Min(chunkSize, filePaths.Count - i)));
            }

            return chunkedFiles;
        }

        static void LogPerformanceData(int maxDegreeOfParallelism, long elapsedMilliseconds)
        {
            string baseDir = "/Users/aleksej/MyProjects/coursework-4/server/paralel 4/results/";
            string logFile = Path.Combine(baseDir, "performance_log.txt");
            string logLine = $"{DateTime.Now}, Threads: {maxDegreeOfParallelism}, Time: {elapsedMilliseconds} ms";
            
            Console.WriteLine(logLine);
            
            try
            {
                File.AppendAllText(logFile, logLine + Environment.NewLine);
            }
            catch (IOException e)
            {
                Console.WriteLine("Не вдалося записати в файл логу: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Відсутній доступ для запису в файл логу: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Сталася помилка при запису в файл логу: " + e.Message);
            }
        }

        static void AddDocumentToIndex(string documentId, string content)
        {
            var words = content.Split(new char[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                InvertedIndex.AddOrUpdate(word, new List<string>() { documentId }, (key, existingValue) =>
                {
                    if (!existingValue.Contains(documentId))
                    {
                        existingValue.Add(documentId);
                    }
                    return existingValue;
                });
            }
        }
        
        static void WriteDictionaryToFile()
        {
            string baseDir = "/Users/aleksej/MyProjects/coursework-4/server/paralel 4/results/";
            string outputFile = Path.Combine(baseDir, "serial_dictionary.txt");

            var sb = new StringBuilder();

            foreach (var pair in InvertedIndex)
            {
                sb.Append(pair.Key).Append(": [");
                sb.Append(string.Join(", ", pair.Value));
                sb.AppendLine("]");
            }

            try
            {
                File.WriteAllText(outputFile, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Помилка при записі словника у файл: " + ex.Message);
            }
        }
    }
}
