using System;
using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4sReaderTests
    {
        [Fact]
        public void Lz4sReader_Preconditions()
        {
            byte[] buffer = new byte[88];

            using (MemoryStream stream = new MemoryStream())
            {
                // Throws due to missing start token
                Assert.Throws<IOException>(() => new Lz4sReader(stream));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (Lz4sWriter writer = new Lz4sWriter(stream, closeStream: false))
                {
                    // Ensure only preamble written
                }

                stream.Seek(0, SeekOrigin.Begin);

                using (Lz4sReader writer = new Lz4sReader(stream))
                {
                    Assert.Throws<ArgumentNullException>(() => writer.Read(null, 0, 0));
                    Assert.Throws<ArgumentOutOfRangeException>(() => writer.Read(buffer, -1, 10));
                    Assert.Throws<ArgumentOutOfRangeException>(() => writer.Read(buffer, 1, -1));
                    Assert.Throws<ArgumentOutOfRangeException>(() => writer.Read(buffer, 1, buffer.Length));

                    writer.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
