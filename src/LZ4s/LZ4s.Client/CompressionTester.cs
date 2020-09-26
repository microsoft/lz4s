using System;
using System.Diagnostics;
using System.IO;

namespace LZ4s.Client
{
    class CompressionTester
    {
        private const string Units = "KB";
        private const double UnitBytes = Kilobyte;

        private const double Kilobyte = 1024;
        private const double Megabyte = 1024 * 1024;

        private readonly byte[] Buffer;
        private readonly string FolderPath;
        private readonly string CompressedFolder;
        private readonly string OutFolder;
        private readonly string Extension;

        private bool AnyFailures;
        private long UncompressedTotalBytes;
        private long CompressedTotalBytes;
        private TimeSpan CompressionTotalTime;
        private TimeSpan DecompressionTotalTime;

        public CompressionTester(string folderPath, string extension)
        {
            Buffer = new byte[Lz4Constants.BufferSize];
            FolderPath = folderPath;
            Extension = extension;

            CompressedFolder = Path.Combine(folderPath, extension);
            OutFolder = Path.Combine(folderPath, "Out");

            Directory.CreateDirectory(CompressedFolder);
            Directory.CreateDirectory(OutFolder);
        }

        public void WriteHeader()
        {
            Console.WriteLine($"Pass?\t{Units}\t{Units}/s\t=>\t{Units}\tRatio\t=>\t{Units}/s\tName");
        }

        public void WriteFooter()
        {
            Console.WriteLine("===================================================================================================");
            Console.WriteLine($"{(AnyFailures ? "FAIL" : "PASS")}\t{(UncompressedTotalBytes / UnitBytes):n2}\t{UncompressedTotalBytes / (UnitBytes * CompressionTotalTime.TotalSeconds):n0}\t=>\t{CompressedTotalBytes / UnitBytes:n2}\t{(1 - ((double)CompressedTotalBytes) / UncompressedTotalBytes):p0}\t=>\t{UncompressedTotalBytes / (UnitBytes * DecompressionTotalTime.TotalSeconds):n0}\t{Extension.ToUpperInvariant()}");
        }

        public void RoundTripFile(FileInfo file, int decompressIterations = 1)
        {
            string compressedPath = Path.Combine(CompressedFolder, file.Name + "." + Extension);
            string outPath = Path.Combine(OutFolder, file.Name);

            Stopwatch cw = Stopwatch.StartNew();
            Lz4sStream.Compress(file.FullName, compressedPath, Buffer);
            cw.Stop();

            CompressionTotalTime += cw.Elapsed;
            UncompressedTotalBytes += file.Length;

            Stopwatch dw = Stopwatch.StartNew();
            for (int i = 0; i < decompressIterations; ++i)
            {
                Lz4sStream.Decompress(compressedPath, outPath, Buffer);
            }
            dw.Stop();

            long compressedLength = new FileInfo(compressedPath).Length;
            DecompressionTotalTime += (dw.Elapsed / decompressIterations);
            CompressedTotalBytes += compressedLength;

            if (!Lz4sStream.VerifyBytesEqual(file.FullName, outPath, out string errorMessage))
            {
                AnyFailures = true;
                Console.WriteLine($"FAIL '{file.Name}: {errorMessage}");
            }
            else
            {
                Console.WriteLine($"PASS\t{(file.Length / UnitBytes):n2}\t{file.Length / (UnitBytes * cw.Elapsed.TotalSeconds):n0}\t=>\t{compressedLength / UnitBytes:n2}\t{(1 - ((double)compressedLength) / file.Length):p0}\t=>\t{decompressIterations * file.Length / (UnitBytes * dw.Elapsed.TotalSeconds):n0}\t{file.Name}");
            }
        }

        public static void CompressFolder(string folderPath, string extension)
        {
            CompressionTester tester = new CompressionTester(folderPath, extension);

            tester.WriteHeader();
            foreach (FileInfo file in new DirectoryInfo(folderPath).GetFiles())
            {
                tester.RoundTripFile(file, decompressIterations: 10);
            }
            tester.WriteFooter();
        }

        public static void DecompressionPerformance(string filePath, string extension, int decompressIterations)
        {
            CompressionTester tester = new CompressionTester(Path.GetDirectoryName(filePath), extension);

            tester.WriteHeader();
            tester.RoundTripFile(new FileInfo(filePath), decompressIterations: decompressIterations);
        }

        private static Stream OpenFile(string filePath, bool preloadIntoMemory)
        {
            if (preloadIntoMemory)
            {
                MemoryStream stream = new MemoryStream();

                using (Stream fileStream = File.OpenRead(filePath))
                {
                    fileStream.CopyTo(stream);
                }

                return stream;
            }
            else
            {
                return File.OpenRead(filePath);
            }
        }
    }
}
