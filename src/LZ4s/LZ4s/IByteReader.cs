using System;

namespace LZ4s
{
    public interface IByteReader : IDisposable
    {
        public int Read(byte[] array, int index, int length);
    }
}
