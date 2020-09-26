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

        [Fact]
        public void RoundTrip_Silesia()
        {
            RoundTripFolder(@"C:\Download\LZ4S_Content\Silesia");
        }

        private void RoundTripFolder(string folderPath)
        {
            byte[] buffer = new byte[Lz4Constants.BufferSize];

            foreach (string filePath in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                RoundTrip(filePath, buffer);
            }
        }

        private void RoundTrip(string filePath, byte[] buffer = null)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            string lz4Directory = Path.Combine(fileDirectory, "LZ4s");
            string outDirectory = Path.Combine(fileDirectory, "Out");
            Directory.CreateDirectory(lz4Directory);
            Directory.CreateDirectory(outDirectory);

            string lz4sPath = Path.Combine(lz4Directory, fileName + ".lz4s");
            string roundTripPath = Path.Combine(outDirectory, fileName);

            buffer ??= new byte[Lz4Constants.BufferSize];
            Compress(filePath, lz4sPath, buffer);
            Decompress(lz4sPath, roundTripPath, buffer);
            Assert.True(Lz4sStream.VerifyBytesEqual(filePath, roundTripPath, out string errorMessage));
        }

        private static void Compress(string filePath, string lz4Destination, byte[] buffer)
        {
            using (Stream source = File.OpenRead(filePath))
            using (Lz4Writer writer = new Lz4Writer(File.Create(lz4Destination)))
            {
                while (true)
                {
                    int lengthRead = source.Read(buffer, 0, buffer.Length);
                    if (lengthRead == 0) { break; }

                    writer.Write(buffer, 0, lengthRead);
                }
            }
        }

        private static void Decompress(string lz4SourcePath, string destinationFilePath, byte[] buffer)
        {
            using (Stream destination = File.Create(destinationFilePath))
            using (Lz4Reader reader = new Lz4Reader(File.OpenRead(lz4SourcePath)))
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
