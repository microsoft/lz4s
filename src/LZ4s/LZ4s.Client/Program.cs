using System;

namespace LZ4s.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string mode = (args.Length > 0 ? args[0] : "compressfolder");
            string extension = (args.Length > 1 ? args[1] : "lz4").TrimStart('.');
            string targetPath = (args.Length > 2 ? args[2] : @"C:\Download\LZ4S_Content");

            switch(mode.ToLowerInvariant())
            {
                case "compressfolder":
                    CompressionTester.CompressFolder(targetPath, extension);
                    break;

                case "decompressPerformance":
                    CompressionTester.DecompressionPerformance(targetPath, extension, 100);
                    break;

                default:
                    Console.WriteLine($"Unknown Mode: '{mode}'. Modes:\r\n\tcompressFolder [lz4|lz4s] [folderPath]\r\n\tdecompressPerformance [lz4|lz4s] [filePath]");
                    break;
            }
        }
    }
}
