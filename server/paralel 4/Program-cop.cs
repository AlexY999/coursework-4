/* using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;

namespace paralel_4;

static class Server
{
    private static readonly int port = 8888; // the port to listen on
    
    private static bool isRunning = true; // a flag to indicate if the server is running
    
    static void Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        listener.Start();
        Console.WriteLine("Server is listening on port {0}...", port);

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

        Console.WriteLine("Server stopped.");
    }


    static void HandleClientRequest(object obj)
    {
        TcpClient client = (TcpClient)obj;

        try
        {
            // Получаем входной поток для чтения данных
            NetworkStream stream = client.GetStream();
            BinaryReader reader = new BinaryReader(stream);
            while (true)
            {
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string jsonData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Перетворення JSON-рядка назад на об'єкт
                DataReceived data = JsonConvert.DeserializeObject<DataReceived>(jsonData);

                // Використання отриманих даних
                int numThreads = data.NumThreads;
                int[,] matrix = data.Matrix;

                // Додаткова обробка отриманих даних...

                Console.WriteLine("Отримано дані:");
                Console.WriteLine("Кількість потоків: " + numThreads);
                Console.WriteLine("Матриця:");
                PrintMatrix(matrix);
                
                // Починаємо обробку запиту в окремому потоці
                Thread taskThread = new Thread(new ParameterizedThreadStart(StartProcessing));
                taskThread.Start(new Tuple<TcpClient, int[,], int>(client, matrix, numThreads));

                // Відправляємо відповідь клієнту про те, що обробка запущена
                byte[] response = System.Text.Encoding.ASCII.GetBytes("Processing started\n");
                stream.Write(response, 0, response.Length);
                
                // Ожидаем завершения обработки в отдельном потоке
                while (taskThread.IsAlive)
                {
                    // Проверяем, есть ли данные в потоке для чтения
                    if (stream.DataAvailable)
                    {
                        // Читаем данные из потока сетевого соединения
                        buffer = new byte[client.ReceiveBufferSize];
                        bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                        string message = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        // Отвечаем клиенту, что обработка еще выполняется
                        response = System.Text.Encoding.ASCII.GetBytes("Processing is in progress, please wait...\n");
                        stream.Write(response, 0, response.Length);
                    }
                }

                // Перетворення матриці у формат JSON
                string jsonMatrix = JsonConvert.SerializeObject(matrix);

                // Відправка рядка JSON на сторону сервера або де потрібно
                // Наприклад, якщо ви використовуєте TCP-сокети, ви можете відправити його через NetworkStream:
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonMatrix + "\n");
                stream.Write(jsonBytes, 0, jsonBytes.Length);

                taskThread.Join();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            // Закриваємо з'єднання з клієнтом
            // client.Close();
            // Console.WriteLine("Client disconnected");
        }
    }
    
    static void StartProcessing(object obj)
    {
        Tuple<TcpClient, int[,], int> data = (Tuple<TcpClient, int[,], int>)obj;
        TcpClient client = data.Item1;
        int[,] matrix = data.Item2;
        int numThreads = data.Item3;

        try
        {
            CalculateDiagonalSumsParallel(numThreads, matrix);
            
            Thread.Sleep(5000);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            // Закрываем соединение с клиентом
            // client.Close();
            Console.WriteLine("StartProcessing finished");
        }
    }
    
    // Сериализация объекта в массив байтов
    public static byte[] Serialize(object obj)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }
    }
    
    static void PrintMatrix(int[,] matrix) {
        var n = matrix.GetLength(0);
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                Console.Write(matrix[i, j] + " ");
            }
            Console.WriteLine();
        }
    }
    
    static void CalculateDiagonalSumsParallel(int numThreads, int[,] matrix)
    {
        // Виконуємо обрахунки многопотоково
        Thread[] threads = new Thread[numThreads];
        for (int i = 0; i < numThreads; i++)
        {
            threads[i] = new Thread(new ParameterizedThreadStart(Calculate));
            threads[i].Start(new Tuple<int, int[,], int>(i, matrix, numThreads));
        }

        // чекаємо завершення всіх потоків
        for (int i = 0; i < numThreads; i++)
        {
            threads[i].Join();
        }
    }


    // static void Calculate(int threadIndex, int numThreads, int[,] matrix)
    static void Calculate(object obj)
    {
        Tuple<int, int[,], int> data = (Tuple<int, int[,], int>)obj;
        int threadIndex = data.Item1;
        int[,] matrix = data.Item2;
        int numThreads = data.Item3;
        
        int size = matrix.GetLength(0);
        int startIndex = threadIndex * (size / numThreads);
        int endIndex = threadIndex == numThreads - 1 ? size : threadIndex * (size / numThreads) + (size / numThreads);

        for (int i = startIndex; i < endIndex; i++)
        {
            int sum = 0;
            for (int j = 1; j < size; j += 2)
            {
                sum += matrix[j, i];
            }
            matrix[i, i] = sum;
        }
    }
}

public class DataReceived
{
    public int NumThreads { get; set; }
    public int[,] Matrix { get; set; }
} */