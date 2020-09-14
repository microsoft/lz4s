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

        private void RoundTrip(string filePath)
        {
            string lz4sPath = filePath + ".lz4s";
            string rtPath = filePath + ".out";

            byte[] buffer = new byte[64 * 1024];
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

            using (Stream expected = File.OpenRead(filePath))
            using (Stream actual = File.OpenRead(rtPath))
            {
                long position = 0;

                byte[] bufferActual = new byte[buffer.Length];

                while (true)
                {
                    int expectedLength = expected.Read(buffer, 0, buffer.Length);
                    int actualLength = actual.Read(bufferActual, 0, bufferActual.Length);

                    Assert.Equal(expectedLength, actualLength);
                    if (expectedLength == 0) { break; }

                    for (int i = 0; i < expectedLength; ++i)
                    {
                        Assert.Equal(buffer[i], bufferActual[i]);
                    }

                    position += expectedLength;
                }
            }
        }
    }
}
