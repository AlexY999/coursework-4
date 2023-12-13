package org.example;

import com.google.gson.Gson;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.Scanner;

public class Client {
    private static boolean waitForResponse = false;

    public static void main(String[] args) {
        try (Socket socket = new Socket("localhost", 8888)) {
            Scanner scanner = new Scanner(System.in);

            System.out.println("Connected to server.");

            Thread messageReaderThread = new Thread(() -> {
                try {
                    BufferedReader in = new BufferedReader(new InputStreamReader(socket.getInputStream()));
                    String message;
                    while ((message = in.readLine()) != null) {
                        if (!waitForResponse) {
                            System.out.println("Message received from server: " + message);
                        } else {
                            System.out.println("Response from server: " + message);
                            waitForResponse = false;
                        }
                    }

                } catch (IOException e) {
                    e.printStackTrace();
                }
            });
            messageReaderThread.start();

            while (true) {
                while (waitForResponse) {
                    try {
                        Thread.sleep(1000); // Sleep for 1 second
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                }

                System.out.println("Select an option:");
                System.out.println("1. Send matrix and number of threads");
                System.out.println("2. Send process check request");
                System.out.println("3. Exit");
                System.out.println("Your choice: ");
                int choice = scanner.nextInt();

                switch (choice) {
                    case 1:
                        sendMatrixAndThreads(socket, scanner);
                        waitForResponse = true;
                        break;
                    case 2:
                        sendProcessCheckRequest(socket);
                        waitForResponse = true;
                        break;
                    case 3:
                        System.out.println("Exiting the program.");
                        return;
                    default:
                        System.out.println("Invalid choice. Please try again.");
                }

                System.out.println();
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private static void sendMatrixAndThreads(Socket socket, Scanner scanner) throws IOException {
        System.out.print("Enter matrix size n: ");
        int n = scanner.nextInt();
        int[][] matrix = new int[n][n];
        System.out.println("Enter matrix elements (row by row):");
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                matrix[i][j] = scanner.nextInt();
            }
        }

        System.out.print("Enter number of threads: ");
        int numThreads = scanner.nextInt();

        Data data = new Data(numThreads, matrix);

        Gson gson = new Gson();
        String jsonData = gson.toJson(data);

        OutputStream outputStream = socket.getOutputStream();
        outputStream.write((jsonData + "\n").getBytes(StandardCharsets.UTF_8));

        System.out.println("Matrix and number of threads sent to the server.");
    }

    private static void sendProcessCheckRequest(Socket socket) throws IOException {
        OutputStream outputStream = socket.getOutputStream();
        String request = "process_check_request";
        outputStream.write((request + "\n").getBytes(StandardCharsets.UTF_8));

        System.out.println("Process check request sent to the server.");
    }
}