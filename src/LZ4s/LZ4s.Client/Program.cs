using System;

namespace LZ4s.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string mode = (args.Length > 0 ? args[0] : "compressfolder");
            string targetPath = (args.Length > 1 ? args[1] : @"C:\Download\LZ4S_Content\Silesia");

            switch(mode.ToLowerInvariant())
            {
                case "compressfolder":
                    CompressionTester.CompressFolder(targetPath);
                    break;

                case "decompressPerformance":
                    CompressionTester.DecompressionPerformance(targetPath, 100);
                    break;

                case "hash":
                    CompressionTester.HashPerformance(targetPath, 50);
                    break;

                default:
                    Console.WriteLine($"Unknown Mode: '{mode}'. Modes:\r\n\tcompressFolder [folderPath]\r\n\tdecompressPerformance [filePath]");
                    break;
            }
        }
    }
}
