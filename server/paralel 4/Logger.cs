using System.Text;

namespace paralel_4;

public static class Logger
{
    public static void LogPerformanceData(int maxDegreeOfParallelism, long elapsedMilliseconds)
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

    public static void WriteDictionaryToFile(ConcurrentDictionary<string, List<string>> invertedIndex)
    {
        string baseDir = "/Users/aleksej/MyProjects/coursework-4/server/paralel 4/results/";
        string outputFile = Path.Combine(baseDir, "serial_dictionary.txt");

        var sb = new StringBuilder();

        foreach (var pair in invertedIndex)
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