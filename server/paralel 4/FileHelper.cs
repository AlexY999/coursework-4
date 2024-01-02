using System;
using System.Collections.Generic;
using System.IO;

namespace paralel_4
{
    static class FileHelper
    {
        public static List<string> GetFilePathsForVariant(int variant)
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
            
            // string logLine = $"{DateTime.Now}, FilePaths count: {filePaths.Count}";
            // Console.WriteLine(logLine);
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
    }
}