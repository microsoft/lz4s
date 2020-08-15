using System;
using System.Collections.Generic;
using System.IO;

namespace LZ4s
{
    public class Lz4sWriter : IDisposable
    {
        private Stream _stream;
        private byte[] _buffer;
        private int _bufferIndex;
        private int _bufferLength;

        private Dictionary<int, int> _map;

        public Lz4sWriter(Stream stream)
        {
            _stream = stream;
            _buffer = new byte[3 * Lz4sConstants.MaximumCopyFromLength];

            Lz4sConstants.Preamble.CopyTo(_buffer, 0);
            _bufferLength += Lz4sConstants.Preamble.Length;
            _buffer[_bufferLength++] = Lz4sConstants.Separator;

            _map = new Dictionary<int, int>();
        }

        public void Write(byte[] array, int index, int length)
        {
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (length < 0 || index + length > array.Length) { throw new ArgumentOutOfRangeException(nameof(length)); }

            if (length + _bufferLength < _buffer.Length)
            {
                Buffer.BlockCopy(array, index, _buffer, _bufferLength, length);
                _bufferLength += length;
            }
        }

        private void Flush()
        {
            int literalCount = 0;

            int next = (int)(_buffer[_bufferIndex + 0] | _buffer[_bufferIndex + 1] << 8 | _buffer[_bufferIndex + 2] << 16 | _buffer[_bufferIndex + 3] << 24);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
