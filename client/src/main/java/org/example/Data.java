package org.example;

import java.io.Serializable;

public class Data implements Serializable {
    private int numThreads;
    private int[][] matrix;

    public Data(int numThreads, int[][] matrix) {
        this.numThreads = numThreads;
        this.matrix = matrix;
    }

    // Додайте геттери та сеттери за потреби
}
