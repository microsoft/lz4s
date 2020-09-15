using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sReader : IDisposable
    {
        private Stream _stream;
        private bool _endOfData;

        // NEXT: Rewrite in terms of buffers. Use uncompressed buffer to avoid need for bounds checks when decoding.
        private Lz4sBuffer _compressedBuffer;
        private Lz4sBuffer _uncompressedBuffer;

        public Lz4sReader(Stream stream, byte[] compressedBuffer = null, byte[] uncompressedBuffer = null)
        {
            _stream = stream;
            _compressedBuffer = new Lz4sBuffer(compressedBuffer);
            _uncompressedBuffer = new Lz4sBuffer(uncompressedBuffer);

            _compressedBuffer.Read(stream);
            _endOfData = (_compressedBuffer.Length == 0);

            if (Lz4sConstants.Preamble.Length < _compressedBuffer.Length)
            {
                for (int i = 0; i < Lz4sConstants.Preamble.Length; ++i)
                {
                    if (_compressedBuffer.Array[i] != Lz4sConstants.Preamble[i])
                    {
                        throw new IOException($"Stream does not have expected LZ4s preamble. At {i:n0}, expected {Lz4sConstants.Preamble[i]}, found {_compressedBuffer.Array[i]}.");
                    }
                }

                _compressedBuffer.Index += Lz4sConstants.Preamble.Length;
            }
        }

        public int Read(byte[] array, int index, int length)
        {
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new IndexOutOfRangeException(nameof(index)); }
            if (length < 0 || index + length > array.Length) { throw new IndexOutOfRangeException(nameof(length)); }

            int totalRead = 0;

            while (true)
            {
                // Copy uncompress bytes to target array
                int bytesRead = _uncompressedBuffer.Write(array, index, length);
                index += bytesRead;
                length -= bytesRead;
                totalRead += bytesRead;

                // If target array full, done
                if (length == 0) { break; }

                // Clear uncompressed buffer for refilling
                _uncompressedBuffer.Shift();

                // If we need more compressed data...
                if (_compressedBuffer.Length < 2)
                {
                    // If there is no more, stop
                    if (_endOfData) { break; }

                    // Otherwise, refill from the file
                    _compressedBuffer.Shift(Lz4sConstants.MaximumCopyFromDistance);
                    int readFromFile = _compressedBuffer.Read(_stream);
                    _endOfData = (readFromFile == 0);
                }

                // Uncompress available data
                while (_compressedBuffer.Length >= 2 && _uncompressedBuffer.RemainingSpace > Lz4sConstants.MaximumTokenLength)
                {
                    if (!ReadToken(_uncompressedBuffer.Array, ref _uncompressedBuffer.End, _uncompressedBuffer.Array.Length)) { break; }
                }
            }

            // Return overall length read
            return totalRead;
        }

        private bool ReadToken(byte[] array, ref int index, int end)
        {
            int tokenStart = _compressedBuffer.Index;

            // Read lengths
            byte literalLength = _compressedBuffer.Array[tokenStart];
            byte copyLength = _compressedBuffer.Array[tokenStart + 1];

            int compressedLength = 2 + literalLength + (copyLength > 0 ? 2 : 0);
            int decompressedLength = literalLength + copyLength;

            if (tokenStart + compressedLength > _compressedBuffer.End)
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
            Buffer.BlockCopy(_compressedBuffer.Array, tokenStart + 2, array, index, literalLength);

            // Read copied bytes
            if (copyLength > 0)
            {
                ushort copyFromOffset = (ushort)(_compressedBuffer.Array[tokenStart + 2 + literalLength] + (_compressedBuffer.Array[tokenStart + 2 + literalLength + 1] << 8));
                Buffer.BlockCopy(_compressedBuffer.Array, tokenStart - copyFromOffset, array, index + literalLength, copyLength);
            }

            _compressedBuffer.Index += compressedLength;
            index += decompressedLength;

            if (decompressedLength == 0)
            {
                _endOfData = true;
                return false;
            }
            else
            {
                return true;
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
