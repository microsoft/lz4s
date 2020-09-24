using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4RoundTripTests
    {
        [Fact]
        public void RoundTrip_Example()
        {
            RoundTrip(@"Content/Example.json");
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

            byte[] buffer = new byte[Lz4Constants.BufferSize];
            Compress(filePath, lz4sPath, buffer);
            Decompress(lz4sPath, roundTripPath, buffer);
            Assert.True(Lz4sStream.VerifyBytesEqual(filePath, roundTripPath, out string errorMessage));
        }

        private static void Compress(string filePath, string lz4Destination, byte[] buffer)
        {
            using (Stream source = File.OpenRead(filePath))
            using (Lz4sWriter writer = new Lz4sWriter(File.Create(lz4Destination)))
            {
                while (true)
                {
                    int lengthRead = source.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        public static void Decompress(string lz4SourcePath, string destinationFilePath, byte[] buffer)
        {
            using (Stream destination = File.Create(destinationFilePath))
            using (Lz4sReader reader = new Lz4sReader(File.OpenRead(lz4SourcePath)))
            {
                while (true)
                {
                    int lengthRead = reader.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    destination.Write(buffer, 0, lengthRead);
                }
            }
        }
    }
}
