using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sReader : IDisposable
    {
        private Stream _stream;
        private byte[] _buffer;
        private int _bufferIndex;
        private int _bufferLength;
        private bool _endOfData;

        public Lz4sReader(Stream stream)
        {
            _stream = stream;

            _buffer = new byte[2 * Lz4sConstants.MaximumCopyFromLength];
            _bufferLength = _stream.Read(_buffer, 0, _buffer.Length);

            for (int i = 0; i < Lz4sConstants.Preamble.Length; ++i)
            {
                if (_buffer[i] != Lz4sConstants.Preamble[i])
                {
                    throw new IOException($"Stream does not have expected LZ4s preamble. At {i:n0}, expected {Lz4sConstants.Preamble[i]}, found {_buffer[i]}.");
                }
            }

            _bufferIndex += Lz4sConstants.Preamble.Length;
            if (_buffer[_bufferIndex] != Lz4sConstants.Separator)
            {
                throw new IOException($"Stream does not have expected LZ4s separator. At {_bufferIndex:n0}, expected {Lz4sConstants.Separator}, found {_buffer[_bufferIndex]}.");
            }
        }

        public int Read(byte[] buffer, int index, int length)
        {
            if (buffer == null) { throw new ArgumentNullException(nameof(buffer)); }
            if (index < 0 || index >= buffer.Length) { throw new IndexOutOfRangeException(nameof(index)); }
            if (length < 0 || index + length > buffer.Length) { throw new IndexOutOfRangeException(nameof(length)); }

            int start = index;
            int end = index + length;

            while (!_endOfData)
            {
                while (_bufferIndex < _bufferLength && index < end)
                {
                    int tokenStart = _bufferIndex;
                    byte literalLength = _buffer[_bufferIndex++];
                    byte copyLength = _buffer[_bufferIndex++];

                    // If token not completely in buffer, read more before decoding
                    if (_bufferIndex + literalLength + copyLength >= _bufferLength)
                    {
                        _bufferIndex -= 2;
                        break;
                    }

                    // Read literal bytes
                    if (literalLength > 0)
                    {
                        Buffer.BlockCopy(_buffer, _bufferIndex, buffer, index, literalLength);
                        _bufferIndex += literalLength;
                        index += literalLength;
                    }

                    // Read copied bytes
                    if (copyLength > 0)
                    {
                        ushort copyFromOffset = (ushort)(_buffer[_bufferIndex] + _buffer[_bufferIndex + 1] << 8);
                        int copyFromPosition = tokenStart - copyFromOffset;
                        if (copyFromPosition < 0)
                        {
                            throw new IOException($"Token at {_stream.Position - (_bufferLength + _bufferIndex):n0} specified out of range copy from, {copyFromOffset:n0}.");
                        }

                        Buffer.BlockCopy(_buffer, copyFromPosition, buffer, index, copyLength);
                        _bufferIndex += 2;
                        index += copyLength;
                    }

                    // If zero of both, end of content
                    if (_bufferIndex == tokenStart)
                    {
                        _endOfData = true;
                        return (index - start);
                    }
                }

                // Normal: Shift to start
                int keepFromIndex = _bufferIndex - Lz4sConstants.MaximumCopyFromLength;
                int keepLength = _bufferLength - keepFromIndex;

                if (keepFromIndex > 0)
                {
                    Buffer.BlockCopy(_buffer, keepFromIndex, _buffer, 0, keepLength);

                    _bufferIndex = Lz4sConstants.MaximumCopyFromLength;
                    _bufferLength = keepLength;
                }

                _bufferLength += _stream.Read(_buffer, keepLength, _buffer.Length - keepLength);
            }

            return (index - start);
        }

        public void Dispose()
        {
            _stream.Dispose();
            _stream = null;
        }
    }
}
