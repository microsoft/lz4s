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
        public void RoundTrip_Others()
        {
            RoundTrip(@"Content/RoundTrip_Code.PNG");
        }

        private void RoundTrip(string filePath)
        {
            string lz4sPath = filePath + ".lz4s";
            string roundTripPath = filePath + ".out";

            byte[] buffer = new byte[Lz4Constants.BufferSize];
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
