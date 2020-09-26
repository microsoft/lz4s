using System;

namespace LZ4s
{
    public interface IByteWriter : IDisposable
    {
        public void Write(byte[] array, int index, int length);
    }
}
