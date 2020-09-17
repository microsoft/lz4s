using System;
using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4sWriterTests
    {
        [Fact]
        public void Lz4sWriter_Preconditions()
        {
            byte[] buffer = new byte[88];

            using (MemoryStream stream = new MemoryStream())
            using (Lz4sWriter writer = new Lz4sWriter(stream))
            {
                Assert.Throws<ArgumentNullException>(() => writer.Write(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(buffer, -1, 10));
                Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(buffer, 1, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(buffer, 1, buffer.Length));

                writer.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
