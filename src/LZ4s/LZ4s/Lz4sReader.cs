using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sReader : IDisposable
    {
        private Stream _stream;
        private bool _endOfData;

        // Buffer of *compressed* content read from file, keeping enough to copy reused bytes (LZ4Constants.MaximumCopyFromDistance)
        private byte[] _buffer;

        // Index of next byte to decompress
        private int _bufferIndex;

        // Index of first byte which doesn't have data loaded
        private int _bufferEnd;

        public Lz4sReader(Stream stream, byte[] buffer = null)
        {
            _stream = stream;
            _buffer = buffer ?? new byte[Lz4sConstants.BufferSize];

            _bufferEnd = _stream.Read(_buffer, 0, _buffer.Length);
            _endOfData = (_bufferEnd == 0);

            if (Lz4sConstants.Preamble.Length < _bufferEnd)
            {
                for (int i = 0; i < Lz4sConstants.Preamble.Length; ++i)
                {
                    if (_buffer[i] != Lz4sConstants.Preamble[i])
                    {
                        throw new IOException($"Stream does not have expected LZ4s preamble. At {i:n0}, expected {Lz4sConstants.Preamble[i]}, found {_buffer[i]}.");
                    }
                }

                _bufferIndex += Lz4sConstants.Preamble.Length;
            }
        }

        public int Read(byte[] array, int index, int length)
        {
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new IndexOutOfRangeException(nameof(index)); }
            if (length < 0 || index + length > array.Length) { throw new IndexOutOfRangeException(nameof(length)); }

            int start = index;
            int end = index + length;

            int bufferStop = _bufferEnd - 2;
            while (_bufferIndex < bufferStop && index < end)
            {
                if (!ReadToken(array, ref index, end)) { break; }
            }

            // TODO: Need to know why we couldn't read more.
            // Only refill if input was why we stopped.
            //RefillBuffer();

            // Return overall length read
            return (index - start);
        }

        private int ReadTokenUnchecked(byte[] array, ref int index)
        {
            // ReadToken when we know that input and output array must be large enough for a maximum length token.

            int tokenStart = _bufferIndex;

            // Read lengths
            byte literalLength = _buffer[_bufferIndex++];
            byte copyLength = _buffer[_bufferIndex++];

            // Read literal bytes
            Buffer.BlockCopy(_buffer, _bufferIndex, array, index, literalLength);
            _bufferIndex += literalLength;
            index += literalLength;

            // Read copied bytes
            if (copyLength > 0)
            {
                ushort copyFromOffset = (ushort)(_buffer[_bufferIndex] + (_buffer[_bufferIndex + 1] << 8));
                Buffer.BlockCopy(_buffer, tokenStart - copyFromOffset, array, index, copyLength);
                _bufferIndex += 2;
                index += copyLength;
            }

            return literalLength + copyLength;
        }

        private bool ReadToken(byte[] array, ref int index, int end)
        {
            int tokenStart = _bufferIndex;

            // Read lengths
            byte literalLength = _buffer[tokenStart];
            byte copyLength = _buffer[tokenStart + 1];

            int compressedLength = 2 + literalLength + (copyLength > 0 ? 2 : 0);
            int decompressedLength = literalLength + copyLength;

            if (tokenStart + compressedLength > _bufferEnd)
            {
                // Compressed token not fully in buffer; must read more
                return false;
            }

            if (index + decompressedLength > end)
            {
                // Not enough room to fully decode token
                return false;
            }

            // Read literal bytes
            Buffer.BlockCopy(_buffer, tokenStart + 2, array, index, literalLength);

            // Read copied bytes
            if (copyLength > 0)
            {
                ushort copyFromOffset = (ushort)(_buffer[tokenStart + 2 + literalLength] + (_buffer[tokenStart + 2 + literalLength + 1] << 8));
                Buffer.BlockCopy(_buffer, tokenStart - copyFromOffset, array, index + literalLength, copyLength);
            }

            _bufferIndex += compressedLength;
            index += decompressedLength;

            return (decompressedLength > 0);
        }

        private void RefillBuffer()
        {
            // When unable to decode more, shift bytes and read to refill buffer.
            // Must keep bytes between _bufferIndex and _bufferEnd, plus MaxCopyFromDistance before _bufferIndex.

            // Buffer:
            // 0        keepFromIndex               _bufferIndex        _bufferEnd    _buffer.Length
            // |        | <- MaxCopyFromDistance -> |                   |             |
            // |        | <-             keepLength                  -> |             |

            int keepFromIndex = _bufferIndex - Lz4sConstants.MaximumCopyFromDistance;
            int keepLength = _bufferEnd - keepFromIndex;

            if (keepFromIndex > 0)
            {
                // Shift by keepFromIndex to keep to beginning of buffer
                Buffer.BlockCopy(_buffer, keepFromIndex, _buffer, 0, keepLength);
                _bufferIndex -= keepFromIndex;
                _bufferEnd = keepLength;

                // Refill end of buffer
                if (!_endOfData)
                {
                    int lengthRead = _stream.Read(_buffer, keepLength, _buffer.Length - keepLength);
                    _bufferEnd += lengthRead;
                    _endOfData = (lengthRead == 0);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
