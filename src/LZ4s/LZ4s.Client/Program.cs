using System;
using System.IO;

namespace LZ4s.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string mode = (args.Length > 0 ? args[0] : "compressfolder");
            string extension = (args.Length > 1 ? args[1] : "lz4").TrimStart('.');
            string sourcePath = (args.Length > 2 ? args[2] : @"C:\Download\Silesia_Corpus");

            string compressedPath = Path.GetFullPath(Path.Combine(sourcePath, $"../Out/{extension}"));
            string outPath = Path.GetFullPath(Path.Combine(sourcePath, $"../Out/{extension}_Out"));

            switch (mode.ToLowerInvariant())
            {
                case "compressfolder":
                    CompressionTester.CompressFolder(extension, sourcePath, compressedPath, outPath);
                    break;

                //case "decompressPerformance":
                //    CompressionTester.DecompressionPerformance(targetPath, extension, 100);
                //    break;

                default:
                    Console.WriteLine($"Unknown Mode: '{mode}'. Modes:\r\n\tcompressFolder [lz4|lz4s] [folderPath]\r\n\tdecompressPerformance [lz4|lz4s] [filePath]");
                    break;
            }
        }
    }
}
