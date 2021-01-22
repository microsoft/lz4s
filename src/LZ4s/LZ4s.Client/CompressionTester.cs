using RoughBench;
using System;
using System.Diagnostics;
using System.IO;

namespace LZ4s.Client
{
    class CompressionTester
    {
        public static void CompressFolder(string extension, string sourcePath, string compressedPath, string outPath)
        {
            byte[] buffer = new byte[Lz4Constants.BufferSize];

            Directory.CreateDirectory(compressedPath);
            Directory.CreateDirectory(outPath);

            ConsoleTable table = new ConsoleTable(
                new TableCell("U. Size", Align.Right),
                new TableCell("C. Size", Align.Right),
                new TableCell("Ratio", Align.Right),
                new TableCell("Compress", Align.Right),
                new TableCell("Decompress", Align.Right),
                new TableCell("Match")
            );

            foreach (FileInfo file in new DirectoryInfo(sourcePath).GetFiles())
            {
                string compressedFilePath = Path.Combine(compressedPath, file.Name + "." + extension);
                string outFilePath = Path.Combine(outPath, file.Name);

                Stopwatch cw = Stopwatch.StartNew();
                Lz4sStream.Compress(file.FullName, compressedFilePath, buffer);
                cw.Stop();

                FileInfo compressedFile = new FileInfo(compressedFilePath);

                Stopwatch dw = Stopwatch.StartNew();
                Lz4sStream.Decompress(compressedFilePath, outFilePath, buffer);
                dw.Stop();

                bool matches = Lz4sStream.VerifyBytesEqual(file.FullName, outFilePath, out string errorMessage);

                table.AppendRow(
                    Format.Size(file.Length),
                    Format.Size(compressedFile.Length),
                    Format.Ratio(file.Length, compressedFile.Length),
                    Format.Rate(file.Length, cw.Elapsed.TotalSeconds),
                    Format.Rate(file.Length, dw.Elapsed.TotalSeconds),
                    (matches ? "PASS" : $"FAIL {errorMessage}")
                );
            }
        }

        public static void DecompressionPerformance(string filePath, string extension, int decompressIterations)
        {
            //CompressionTester tester = new CompressionTester(Path.GetDirectoryName(filePath), extension);

            //tester.WriteHeader();
            //tester.RoundTripFile(new FileInfo(filePath), decompressIterations: decompressIterations);
        }
    }
}
