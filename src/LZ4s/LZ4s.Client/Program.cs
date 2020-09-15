using System;
using System.Diagnostics;
using System.IO;

namespace LZ4s.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = @"C:\Download\Example.10MB.json";
            int iterations = 100;
            long targetSizeBytes = new FileInfo(filePath).Length;

            string lz4sPath = filePath + ".lz4s";
            string roundTripPath = filePath + ".out";
            byte[] buffer = new byte[64 * 1024];

            Stopwatch w = Stopwatch.StartNew();
            if (!File.Exists(lz4sPath))
            {
                Compress(filePath, lz4sPath, buffer);
                w.Stop();
                Console.WriteLine($"Compressed {targetSizeBytes:n0} bytes in {w.ElapsedMilliseconds} ms");

                Decompress(lz4sPath, roundTripPath, buffer);
                VerifyFileBytesEqual(filePath, roundTripPath);
            }

            w.Restart();
            for (int i = 0; i < iterations; ++i)
            {
                Decompress(lz4sPath, roundTripPath, buffer);
            }
            w.Stop();
            Console.WriteLine($"Decompressed {targetSizeBytes:n0} bytes {iterations}x in {w.Elapsed.TotalSeconds:n3} sec.");
        }

        private void RoundTrip(string filePath)
        {
            string lz4sPath = filePath + ".lz4s";
            string roundTripPath = filePath + ".out";

            byte[] buffer = new byte[64 * 1024];
            Compress(filePath, lz4sPath, buffer);
            Decompress(lz4sPath, roundTripPath, buffer);
            VerifyFileBytesEqual(filePath, roundTripPath);
        }

        private static void Decompress(string lz4sPath, string rtPath, byte[] buffer)
        {
            using (Lz4sReader reader = new Lz4sReader(File.OpenRead(lz4sPath)))
            using (Stream writer = File.Create(rtPath))
            {
                while (true)
                {
                    int lengthRead = reader.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        private static void Compress(string filePath, string lz4sPath, byte[] buffer)
        {
            using (Stream reader = File.OpenRead(filePath))
            using (Lz4sWriter writer = new Lz4sWriter(File.Create(lz4sPath)))
            {
                while (true)
                {
                    int lengthRead = reader.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        private static void VerifyFileBytesEqual(string expectedPath, string actualPath)
        {
            using (Stream expected = File.OpenRead(expectedPath))
            using (Stream actual = File.OpenRead(actualPath))
            {
                if (expected.Length != actual.Length)
                {
                    Console.WriteLine($"Error: '{actualPath}' was {actual.Length:n0} bytes, but expected '{expectedPath}' was {expected.Length:n0} bytes.");
                    return;
                }

                long position = 0;

                byte[] bufferExpected = new byte[32 * 1024];
                byte[] bufferActual = new byte[bufferExpected.Length];

                while (true)
                {
                    int expectedLength = expected.Read(bufferExpected, 0, bufferExpected.Length);
                    int actualLength = actual.Read(bufferActual, 0, bufferActual.Length);

                    if (expectedLength == 0) { break; }

                    for (int i = 0; i < expectedLength; ++i)
                    {
                        if (bufferExpected[i] != bufferActual[i])
                        {
                            Console.WriteLine($"Error: At {position + i:n0}, expected {bufferExpected[i]} but read {bufferActual[i]}.");
                            return;
                        }
                    }

                    position += expectedLength;
                }
            }

            Console.WriteLine($"PASS. '{actualPath}' is identical to '{expectedPath}'.");
        }

        private void BuildRoundTripLarge(string inputFilePath, string outputFilePath, int targetSizeBytes)
        {
            if (File.Exists(outputFilePath)) { return; }

            byte[] fileData = File.ReadAllBytes(inputFilePath);

            long bytesWritten = 0;
            using (Stream stream = File.Create(outputFilePath))
            {
                while (bytesWritten < targetSizeBytes)
                {
                    stream.Write(fileData, 0, fileData.Length);
                    bytesWritten += fileData.Length;
                }
            }
        }
    }
}
