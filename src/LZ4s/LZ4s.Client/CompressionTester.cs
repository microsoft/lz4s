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
                new TableCell("Name"),
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

                // To buffer in memory only. Minimal performance difference observed.
                //using (Stream source = LoadToMemory(file.FullName))
                //using (Stream compressed = new MemoryStream((int)file.Length))
                //using (Stream uncompressed = new MemoryStream((int)file.Length))

                using (Stream source = File.OpenRead(file.FullName))
                using (Stream compressed = File.Create(compressedFilePath))
                using (Stream uncompressed = File.Create(outFilePath))
                {
                    Stopwatch cw = Stopwatch.StartNew();
                    Lz4sStream.Compress(extension, source, compressed, buffer, closeStream: false);
                    cw.Stop();

                    long compressedLength = compressed.Length;
                    compressed.Seek(0, SeekOrigin.Begin);
                    
                    Stopwatch dw = Stopwatch.StartNew();
                    Lz4sStream.Decompress(extension, compressed, uncompressed, buffer, closeStream: false);
                    dw.Stop();

                    bool matches = Lz4sStream.VerifyBytesEqual(source, uncompressed, file.FullName, outFilePath, out string errorMessage);

                    table.AppendRow(
                        file.Name,
                        Format.Size(file.Length),
                        Format.Size(compressedLength),
                        Format.Ratio(file.Length, compressedLength),
                        Format.Rate(file.Length, cw.Elapsed.TotalSeconds),
                        Format.Rate(file.Length, dw.Elapsed.TotalSeconds),
                        (matches ? "PASS" : $"FAIL {errorMessage}")
                    );
                }
            }
        }

        public static void DecompressionPerformance(string filePath, string extension, int decompressIterations)
        {
            //CompressionTester tester = new CompressionTester(Path.GetDirectoryName(filePath), extension);

            //tester.WriteHeader();
            //tester.RoundTripFile(new FileInfo(filePath), decompressIterations: decompressIterations);
        }

        private static Stream LoadToMemory(string filePath)
        {
            MemoryStream ms = new MemoryStream();
            using (Stream file = File.OpenRead(filePath))
            {
                file.CopyTo(ms);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
