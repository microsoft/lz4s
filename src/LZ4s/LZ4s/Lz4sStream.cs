using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sStream
    {
        // TODO: Inherit stream, wrap Reader/Writer

        public static IByteWriter BuildWriter(string extension, Stream stream, bool closeStream = true)
        {
            switch (extension.TrimStart('.').ToLowerInvariant())
            {
                case "lz4s":
                    return new Lz4sWriter(stream, closeStream);
                case "lz4":
                    return new Lz4Writer(stream, closeStream);
                default:
                    throw new NotImplementedException($"Lz4sStream does not know how to build writer for extension '{extension}'");
            }
        }

        public static IByteReader BuildReader(string extension, Stream stream, bool closeStream = true)
        {
            switch (extension.TrimStart('.').ToLowerInvariant())
            {
                case "lz4s":
                    return new Lz4sReader(stream, closeStream);
                case "lz4":
                    return new Lz4Reader(stream, closeStream);
                default:
                    throw new NotImplementedException($"Lz4sStream does not know how to build reader for extension '{extension}'");
            }
        }

        public static void Compress(string sourceFilePath, string compressedPath, byte[] buffer = null)
        {
            using (Stream source = File.OpenRead(sourceFilePath))
            using (IByteWriter writer = BuildWriter(Path.GetExtension(compressedPath), File.Create(compressedPath)))
            {
                Compress(source, writer, buffer);
            }
        }

        public static void Compress(string extension, Stream source, Stream destination, byte[] buffer = null, bool closeStream = true)
        {
            Compress(source, BuildWriter(extension, destination, closeStream), buffer);
        }

        private static void Compress(Stream source, IByteWriter writer, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

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
            using (IByteReader reader = BuildReader(Path.GetExtension(compressedPath), File.OpenRead(compressedPath)))
            using (Stream destination = File.Create(destinationFilePath))
            {
                Decompress(reader, destination, buffer);
            }
        }

        public static void Decompress(string extension, Stream source, Stream destination, byte[] buffer = null, bool closeStream = true)
        {
            Decompress(
                BuildReader(extension, source, closeStream),
                destination,
                buffer
            );
        }

        private static void Decompress(IByteReader reader, Stream destination, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

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
            using (Stream expected = File.OpenRead(expectedFilePath))
            using (Stream actual = File.OpenRead(actualFilePath))
            {
                return VerifyBytesEqual(expected, actual, expectedFilePath, actualFilePath, out errorMessage);
            }
        }

        public static bool VerifyBytesEqual(Stream expected, Stream actual, string expectedName, string actualName, out string errorMessage)
        {
            errorMessage = null;

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
                    errorMessage = $"Error: Data ended at {(position + actualLength):n0} bytes, but {expected.Length:n0} total bytes expected.";
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

            return true;
        }
    }
}
