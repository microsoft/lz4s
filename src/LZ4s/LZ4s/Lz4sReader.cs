using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sReader : IByteReader, IDisposable
    {
        private Stream _stream;
        private bool _closeStream;
        private bool _endOfData;

        // NEXT: Rewrite in terms of buffers. Use uncompressed buffer to avoid need for bounds checks when decoding.
        private FileBuffer _compressedBuffer;
        private FileBuffer _uncompressedBuffer;

        public Lz4sReader(Stream stream, bool closeStream = true, byte[] compressedBuffer = null, byte[] uncompressedBuffer = null)
        {
            _stream = stream;
            _closeStream = closeStream;
            _compressedBuffer = new FileBuffer(compressedBuffer);
            _uncompressedBuffer = new FileBuffer(uncompressedBuffer);

            _compressedBuffer.AppendFrom(stream);
            _endOfData = (_compressedBuffer.Length == 0);

            if (!HasPreamble())
            {
                throw new IOException("Stream does not start with required LZ4s preamble.");
            }
        }

        public int Read(byte[] array, int index, int length)
        {
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (length < 0 || index + length > array.Length) { throw new ArgumentOutOfRangeException(nameof(length)); }

            int totalRead = 0;

            while (true)
            {
                // Uncompress until we have the requested amount or run out of compressed data
                while (_uncompressedBuffer.Length < length && _compressedBuffer.Length >= 2)
                {
                    if (!ReadToken(_uncompressedBuffer.Array, ref _uncompressedBuffer.End, _uncompressedBuffer.Array.Length)) { break; }
                }

                // Copy what we have to the output array
                int bytesRead = _uncompressedBuffer.WriteTo(array, index, length);
                index += bytesRead;
                length -= bytesRead;
                totalRead += bytesRead;

                // If we fulfilled the request or decompressed all file data, stop
                if (length == 0 || _endOfData) { break; }

                // Otherwise, make buffer space and read more from the file
                _uncompressedBuffer.Shift();
                _compressedBuffer.Shift(Constants.MaximumCopyFromDistance);
                _compressedBuffer.AppendFrom(_stream);

                // If everything was previously read and decoded, we're out of data
                _endOfData = (_compressedBuffer.Length == 0);
            }

            // Return overall length read
            return totalRead;
        }

        private bool ReadToken(byte[] array, ref int index, int end)
        {
            byte[] source = _compressedBuffer.Array;
            int tokenStart = _compressedBuffer.Index;

            // Read lengths
            byte literalLength = source[tokenStart];
            byte copyLength = source[tokenStart + 1];

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
            Helpers.ArrayCopy(source, tokenStart + 2, array, index, literalLength);

            // Read copied bytes
            if (copyLength > 0)
            {
                ushort copyFromOffset = (ushort)(source[tokenStart + 2 + literalLength] + (source[tokenStart + 2 + literalLength + 1] << 8));
                Helpers.ArrayCopy(source, tokenStart - copyFromOffset, array, index + literalLength, copyLength);
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

        private bool HasPreamble()
        {
            if (_compressedBuffer.Length < Constants.Preamble.Length) { return false; }

            for (int i = 0; i < Constants.Preamble.Length; ++i)
            {
                if (_compressedBuffer.Array[i] != Constants.Preamble[i])
                {
                    return false;
                }
            }

            _compressedBuffer.Index += Constants.Preamble.Length;
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_closeStream) { _stream?.Dispose(); }
            _stream = null;
        }
    }
}
