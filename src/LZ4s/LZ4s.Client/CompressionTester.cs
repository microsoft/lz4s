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
        private readonly string Lz4sFolder;
        private readonly string OutFolder;

        public CompressionTester(string folderPath)
        {
            Buffer = new byte[Constants.BufferSize];
            FolderPath = folderPath;

            Lz4sFolder = Path.Combine(folderPath, "LZ4s");
            OutFolder = Path.Combine(folderPath, "Out");

            Directory.CreateDirectory(Lz4sFolder);
            Directory.CreateDirectory(OutFolder);
        }

        public void WriteHeader()
        {
            Console.WriteLine($"Pass?\t{Units}\t{Units}/s\t=>\t{Units}\tRatio\t=>\t{Units}/s\tName");
        }

        public void RoundTripFile(FileInfo file, int decompressIterations = 1)
        {
            string compressedPath = Path.Combine(Lz4sFolder, file.Name + ".lz4s");
            string outPath = Path.Combine(OutFolder, file.Name);

            Stopwatch cw = Stopwatch.StartNew();
            Lz4sStream.Compress(file.FullName, compressedPath, Buffer);
            cw.Stop();

            Stopwatch dw = Stopwatch.StartNew();
            for (int i = 0; i < decompressIterations; ++i)
            {
                Lz4sStream.Decompress(compressedPath, outPath, Buffer);
            }
            dw.Stop();

            if (!Lz4sStream.VerifyBytesEqual(file.FullName, outPath, out string errorMessage))
            {
                Console.WriteLine($"FAIL '{file.Name}: {errorMessage}");
            }
            else
            {
                long compressedLength = new FileInfo(compressedPath).Length;
                Console.WriteLine($"PASS\t{(file.Length / UnitBytes):n2}\t{file.Length / (UnitBytes * cw.Elapsed.TotalSeconds):n0}\t=>\t{compressedLength / UnitBytes:n2}\t{(1 - ((double)compressedLength) / file.Length):p0}\t=>\t{decompressIterations * file.Length / (UnitBytes * dw.Elapsed.TotalSeconds):n0}\t{file.Name}");
            }
        }

        public static void CompressFolder(string folderPath)
        {
            CompressionTester tester = new CompressionTester(folderPath);

            tester.WriteHeader();
            foreach (FileInfo file in new DirectoryInfo(folderPath).GetFiles())
            {
                tester.RoundTripFile(file, decompressIterations: 10);
            }
        }

        public static void DecompressionPerformance(string filePath, int decompressIterations)
        {
            CompressionTester tester = new CompressionTester(Path.GetDirectoryName(filePath));

            tester.WriteHeader();
            tester.RoundTripFile(new FileInfo(filePath), decompressIterations: decompressIterations);
        }
    }
}
