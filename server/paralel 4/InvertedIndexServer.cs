using System.Net;
using System.Net.Sockets;

namespace paralel_4;

static class InvertedIndexServer
{
    private static readonly int port = 8888;
    private static readonly int variant = 27;
    private static bool isRunning = true;
    private static readonly Dictionary<string, List<string>> invertedIndex = new Dictionary<string, List<string>>();

    static void Main(string[] args)
    {
        Console.WriteLine("Starting Inverted Index server...");

        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        listener.Start();
        Console.WriteLine("Server is listening on port {0}...", port);
        
        BuildInvertedIndexForVariant(variant);

        Thread thread = new Thread(() =>
        {
            while (isRunning)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected from {0}", client.Client.RemoteEndPoint);

                Thread clientThread = new Thread(() => HandleClientRequest(client));
                clientThread.Start();
            }
        });
        thread.Start();

        Console.WriteLine("Press any key to stop the server.");
        Console.ReadKey();

        isRunning = false;
        thread.Join();
        listener.Stop();

        Console.WriteLine("Server stopped.");
    }

    static void HandleClientRequest(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            // Отримання запиту від клієнта
            string keyword = reader.ReadLine();

            // Пошук документів з використанням інвертованого індексу
            var foundDocuments = SearchInInvertedIndex(keyword);

            // Відправка результатів пошуку назад клієнту
            string response = foundDocuments.Count > 0
                ? $"Found in documents: {string.Join(", ", foundDocuments)}"
                : "No documents found.";

            writer.WriteLine(response);
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
        if (invertedIndex.ContainsKey(keyword))
        {
            return invertedIndex[keyword];
        }
        return new List<string>();
    }


    static void BuildInvertedIndexForVariant(int variant)
    {
        var filePaths = GetFilePathsForVariant(variant);
        foreach (var filePath in filePaths)
        {
            var content = File.ReadAllText(filePath);
            var documentId = Path.GetFileName(filePath);
            AddDocumentToIndex(documentId, content);
        }
    }

    static List<string> GetFilePathsForVariant(int variant)
    {
        int n1 = 12500, n2 = 50000;
        int startIndex = n1 / 50 * (variant - 1);
        int endIndex = n1 / 50 * variant;

        List<string> filePaths = new List<string>();
        string baseDir = "/Users/aleksej/MyProjects/coursework-4/server/paralel 4/aclImdb";

        filePaths.AddRange(GetFilesFromDirectory(Path.Combine(baseDir, "test", "neg"), startIndex, endIndex));
        filePaths.AddRange(GetFilesFromDirectory(Path.Combine(baseDir, "test", "pos"), startIndex, endIndex));
        filePaths.AddRange(GetFilesFromDirectory(Path.Combine(baseDir, "train", "neg"), startIndex, endIndex));
        filePaths.AddRange(GetFilesFromDirectory(Path.Combine(baseDir, "train", "pos"), startIndex, endIndex));

        startIndex = n2 / 50 * (variant - 1);
        endIndex = n2 / 50 * variant;
        filePaths.AddRange(GetFilesFromDirectory(Path.Combine(baseDir, "train", "unsup"), startIndex, endIndex));

        return filePaths;
    }

    static IEnumerable<string> GetFilesFromDirectory(string dir, int start, int end)
    {
        var files = Directory.GetFiles(dir);
        for (int i = start; i < end && i < files.Length; i++)
        {
            yield return files[i];
        }
    }

    static void AddDocumentToIndex(string documentId, string content)
    {
        var words = content.Split(new char[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (!invertedIndex.ContainsKey(word))
            {
                invertedIndex[word] = new List<string>();
            }
            if (!invertedIndex[word].Contains(documentId))
            {
                invertedIndex[word].Add(documentId);
            }
        }
    }
}