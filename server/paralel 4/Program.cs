using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace paralel_4
{
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

                    TaskCompletionSource<ProcessingResult> processingTaskCompletionSource = new TaskCompletionSource<ProcessingResult>();

                    // Починаємо обробку запиту в окремому потоці
                    Task.Run(() => ProcessRequest(matrix, numThreads, processingTaskCompletionSource));

                    // Відправляємо відповідь клієнту про те, що обробка запущена
                    byte[] response = Encoding.ASCII.GetBytes("Processing started\n");
                    stream.Write(response, 0, response.Length);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    while (!processingTaskCompletionSource.Task.IsCompleted)
                    {
                        // Проверяем, есть ли данные в потоке для чтения
                        if (stream.DataAvailable)
                        {
                            // Читаем данные из потока сетевого соединения
                            buffer = new byte[client.ReceiveBufferSize];
                            bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                            TimeSpan elapsedTime = stopwatch.Elapsed;
                            // Отвечаем клиенту, что обработка еще выполняется
                            response = Encoding.ASCII.GetBytes("Processing is in progress(" + (elapsedTime.Milliseconds) + " ms), please wait...\n");
                            stream.Write(response, 0, response.Length);
                        }
                    }

                    stopwatch.Stop();

                    // Получение результата обработки
                    ProcessingResult result = processingTaskCompletionSource.Task.Result;

                    // Перетворення матриці у формат JSON
                    string jsonMatrix = JsonConvert.SerializeObject(result.Matrix);

                    // Відправка рядка JSON на сторону сервера або де потрібно
                    // Наприклад, якщо ви використовуєте TCP-сокети, ви можете відправити його через NetworkStream:
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonMatrix + "\n");
                    stream.Write(jsonBytes, 0, jsonBytes.Length);

                    // Отправка статуса обработки клиенту
                    string status = string.Format("Processing finished. Diagonal sums: {0}\n", string.Join(", ", result.DiagonalSums));
                    byte[] statusBytes = Encoding.ASCII.GetBytes(status);
                    stream.Write(statusBytes, 0, statusBytes.Length);
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

        static void ProcessRequest(int[,] matrix, int numThreads, TaskCompletionSource<ProcessingResult> taskCompletionSource)
        {
            int size = matrix.GetLength(0);
            int[] diagonalSums = new int[size];

            Thread.Sleep(7000);

            // Виконуємо обрахунки многопотоково
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(Calculate));
                threads[i].Start(new Tuple<int, int[,], int, int[]>(i, matrix, numThreads, diagonalSums));
            }

            // чекаємо завершення всіх потоків
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }

            // ProcessingResult
            ProcessingResult result = new ProcessingResult { Matrix = matrix, DiagonalSums = diagonalSums };

            taskCompletionSource.SetResult(result);
        }

        static void Calculate(object obj)
        {
            Tuple<int, int[,], int, int[]> data = (Tuple<int, int[,], int, int[]>)obj;
            int threadIndex = data.Item1;
            int[,] matrix = data.Item2;
            int numThreads = data.Item3;
            int[] diagonalSums = data.Item4;

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
                diagonalSums[i] = sum;
            }
        }
        
        static void PrintMatrix(int[,] matrix)
        {
            var n = matrix.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    Console.Write(matrix[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
    
    class ProcessingResult
    {
        public int[,] Matrix { get; set; }
        public int[] DiagonalSums { get; set; }
    }

    class DataReceived
    {
        public int NumThreads { get; set; }
        public int[,] Matrix { get; set; }
    }
}
