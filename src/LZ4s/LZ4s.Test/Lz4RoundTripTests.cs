using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4RoundTripTests
    {
        [Theory]
        [InlineData("lz4", @"Content/Example.json")]
        [InlineData("lz4", @"Content/RoundTrip_Code.png")]
        [InlineData("lz4s", @"Content/Example.json")]
        [InlineData("lz4s", @"Content/RoundTrip_Code.png")]
        public void RoundTrip(string extension, string filePath)
        {
            RoundTripFile(filePath, extension);
        }

        //[Fact]
        //public void RoundTrip_Silesia()
        //{
        //    RoundTripFolder(@"C:\Download\LZ4S_Content\Silesia", "lz4");
        //}

        private void RoundTripFolder(string folderPath, string extension)
        {
            byte[] buffer = new byte[Lz4Constants.BufferSize];

            foreach (string filePath in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                RoundTripFile(filePath, extension, buffer);
            }
        }

        private void RoundTripFile(string filePath, string extension, byte[] buffer = null)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            string lz4Directory = Path.Combine(fileDirectory, extension.ToUpper());
            string outDirectory = Path.Combine(fileDirectory, "Out");
            Directory.CreateDirectory(lz4Directory);
            Directory.CreateDirectory(outDirectory);

            // Important: LZ4 file extension is what selects Lz4Reader/Writer
            string lz4sPath = Path.Combine(lz4Directory, fileName + "." + extension);
            string roundTripPath = Path.Combine(outDirectory, fileName);

            buffer ??= new byte[Lz4Constants.BufferSize];
            Lz4sStream.Compress(filePath, lz4sPath, buffer);
            Lz4sStream.Decompress(lz4sPath, roundTripPath, buffer);
            Assert.True(Lz4sStream.VerifyBytesEqual(filePath, roundTripPath, out string errorMessage));
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
