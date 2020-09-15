using System;
using System.Diagnostics;
using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class RoundTripTests
    {
        [Fact]
        public void RoundTrip_Example()
        {
            RoundTrip(@"Content/Example.json");
        }

        [Fact]
        public void RoundTrip_Large()
        {
            string filePath = @"Content/Example.10MB.json";
            int targetSizeBytes = 10 * 1024 * 1024;
            int iterations = 10;
            BuildRoundTripLarge(@"Content/Example.json", filePath, targetSizeBytes);

            string lz4sPath = filePath + ".lz4s";
            string roundTripPath = filePath + ".out";
            byte[] buffer = new byte[64 * 1024];

            Stopwatch w = Stopwatch.StartNew();
            Compress(filePath, lz4sPath, buffer);
            w.Stop();
            Trace.WriteLine($"Compressed {targetSizeBytes:n0} in {w.ElapsedMilliseconds} ms");

            Decompress(lz4sPath, roundTripPath, buffer);
            VerifyFileBytesEqual(filePath, roundTripPath);

            w.Restart();
            for (int i = 0; i < iterations; ++i)
            {
                Decompress(lz4sPath, roundTripPath, buffer);
            }
            w.Stop();
            Trace.WriteLine($"Decompressed {targetSizeBytes:n0} {iterations}x in {w.Elapsed.TotalSeconds:n0} sec.");
        }

        [Fact]
        public void RoundTrip_Others()
        {
            RoundTrip(@"Content/RoundTrip_Code.PNG");
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
                long position = 0;

                byte[] bufferExpected = new byte[32 * 1024];
                byte[] bufferActual = new byte[bufferExpected.Length];

                while (true)
                {
                    int expectedLength = expected.Read(bufferExpected, 0, bufferExpected.Length);
                    int actualLength = actual.Read(bufferActual, 0, bufferActual.Length);

                    Assert.Equal(expectedLength, actualLength);
                    if (expectedLength == 0) { break; }

                    for (int i = 0; i < expectedLength; ++i)
                    {
                        Assert.Equal(bufferExpected[i], bufferActual[i]);
                    }

                    position += expectedLength;
                }
            }
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
