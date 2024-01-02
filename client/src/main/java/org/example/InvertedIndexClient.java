package org.example;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.Socket;
import java.util.Scanner;

public class InvertedIndexClient {
    private static final String SERVER_ADDRESS = "127.0.0.1";
    private static final int SERVER_PORT = 8888;

    public static void main(String[] args) {
        try (Socket socket = new Socket(SERVER_ADDRESS, SERVER_PORT);
             PrintWriter out = new PrintWriter(socket.getOutputStream(), true);
             BufferedReader in = new BufferedReader(new InputStreamReader(socket.getInputStream()));
             Scanner scanner = new Scanner(System.in)) {

            while (true) {
                System.out.println("Enter a keyword to search (or type 'exit' to quit):");
                String keyword = scanner.nextLine();

                if ("exit".equalsIgnoreCase(keyword)) {
                    break;
                }

                // Відправка ключового слова на сервер
                out.println(keyword);

                // Отримання і виведення відповіді від сервера
                String response = in.readLine();
                System.out.println("Server response: " + response);
            }

        } catch (Exception e) {
            System.err.println("Error occurred: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
