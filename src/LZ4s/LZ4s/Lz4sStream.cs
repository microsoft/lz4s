using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sStream
    {
        // TODO: Inherit stream, wrap Reader/Writer

        private static IByteWriter BuildWriter(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch(extension)
            {
                case ".lz4s":
                    return new Lz4sWriter(File.Create(filePath));
                case ".lz4":
                    return new Lz4Writer(File.Create(filePath));
                default:
                    throw new NotImplementedException($"Lz4sStream does not know how to build writer for extension '{extension}'");
            }
        }

        private static IByteReader BuildReader(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".lz4s":
                    return new Lz4sReader(File.OpenRead(filePath));
                case ".lz4":
                    return new Lz4Reader(File.OpenRead(filePath));
                default:
                    throw new NotImplementedException($"Lz4sStream does not know how to build reader for extension '{extension}'");
            }
        }

        public static void Compress(string sourceFilePath, string compressedPath, byte[] buffer = null)
        {
            Compress(File.OpenRead(sourceFilePath), BuildWriter(compressedPath), buffer);
        }

        private static void Compress(Stream source, IByteWriter writer, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

            using (source)
            using (writer)
            {
                while (true)
                {
                    int lengthRead = source.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        public static void Decompress(string compressedPath, string destinationFilePath, byte[] buffer = null)
        {
            Decompress(BuildReader(compressedPath), File.Create(destinationFilePath), buffer);
        }

        private static void Decompress(IByteReader reader, Stream destination, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

            using (destination)
            using (reader)
            {
                while (true)
                {
                    int lengthRead = reader.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    destination.Write(buffer, 0, lengthRead);
                }
            }
        }

        public static bool VerifyBytesEqual(string expectedFilePath, string actualFilePath, out string errorMessage)
        {
            return VerifyBytesEqual(File.OpenRead(expectedFilePath), File.OpenRead(actualFilePath), expectedFilePath, actualFilePath, out errorMessage);
        }

        public static bool VerifyBytesEqual(Stream expected, Stream actual, string expectedName, string actualName, out string errorMessage)
        {
            errorMessage = null;

            using (expected)
            using (actual)
            {
                expected.Seek(0, SeekOrigin.Begin);
                actual.Seek(0, SeekOrigin.Begin);

                long position = 0;
                byte[] bufferExpected = new byte[Constants.BufferSize];
                byte[] bufferActual = new byte[bufferExpected.Length];

                while (true)
                {
                    int expectedLength = expected.Read(bufferExpected, 0, bufferExpected.Length);
                    int actualLength = actual.Read(bufferActual, 0, bufferActual.Length);

                    if (expectedLength != actualLength)
                    {
                        errorMessage = $"Error: Data ended at {(position + actualLength):n0} bytes, bute {expected.Length:n0} total bytes expected.";
                        return false;
                    }

                    if (expectedLength == 0) { break; }

                    for (int i = 0; i < expectedLength; ++i)
                    {
                        if (bufferExpected[i] != bufferActual[i])
                        {
                            errorMessage = $"Error: At {position + i:n0}, expected {bufferExpected[i]} but read {bufferActual[i]}.";
                            return false;
                        }
                    }

                    position += expectedLength;
                }
            }

            return true;
        }
    }
}
