using System.IO;

namespace LZ4s
{
    public class Lz4sStream
    {
        // TODO: Inherit stream, wrap Reader/Writer

        public static void Compress(string sourceFilePath, string lz4sPath, byte[] buffer = null)
        {
            Compress(File.OpenRead(sourceFilePath), File.Create(lz4sPath), buffer);
        }

        public static void Compress(Stream source, Stream lz4sDestination, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

            using (source)
            using (Lz4sWriter writer = new Lz4sWriter(lz4sDestination))
            {
                while (true)
                {
                    int lengthRead = source.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        public static void Decompress(string lz4sPath, string destinationFilePath, byte[] buffer = null)
        {
            Decompress(File.OpenRead(lz4sPath), File.Create(destinationFilePath), buffer);
        }

        public static void Decompress(Stream lz4sSource, Stream destination, byte[] buffer = null)
        {
            buffer ??= new byte[Constants.BufferSize];

            using (destination)
            using (Lz4sReader reader = new Lz4sReader(lz4sSource))
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

                if (expected.Length != actual.Length)
                {
                    errorMessage = $"Error: '{actualName}' was {actual.Length:n0} bytes, but expected '{expectedName}' was {expected.Length:n0} bytes.";
                    return false;
                }

                long position = 0;
                byte[] bufferExpected = new byte[Constants.BufferSize];
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
