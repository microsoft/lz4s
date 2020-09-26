using System;
using System.IO;

namespace LZ4s
{
    public class Lz4Reader : IDisposable
    {
        private Stream _stream;
        private bool _closeStream;
        private bool _endOfData;

        private Lz4sBuffer _compressedBuffer;
        private Lz4sBuffer _uncompressedBuffer;

        public Lz4Reader(Stream stream, bool closeStream = true, byte[] compressedBuffer = null, byte[] uncompressedBuffer = null)
        {
            _stream = stream;
            _closeStream = closeStream;
            _compressedBuffer = new Lz4sBuffer(compressedBuffer);
            _uncompressedBuffer = new Lz4sBuffer(uncompressedBuffer);

            _compressedBuffer.Read(stream);
            _endOfData = (_compressedBuffer.Length == 0);

            //if (!HasPreamble())
            //{
            //    throw new IOException("Stream does not start with required LZ4s preamble.");
            //}
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
                int bytesRead = _uncompressedBuffer.Write(array, index, length);
                index += bytesRead;
                length -= bytesRead;
                totalRead += bytesRead;

                // If we fulfilled the request or decompressed all file data, stop
                if (length == 0 || _endOfData) { break; }

                // Otherwise, make buffer space and read more from the file
                _uncompressedBuffer.Shift();
                _compressedBuffer.Shift(Lz4Constants.MaximumCopyFromDistance);
                int readFromFile = _compressedBuffer.Read(_stream);
                _endOfData = (readFromFile == 0);
            }

            // Return overall length read
            return totalRead;
        }

        private bool ReadToken(byte[] array, ref int index, int end)
        {
            if (_compressedBuffer.Length < 2) { return false; }

            byte[] source = _compressedBuffer.Array;
            int tokenStart = _compressedBuffer.Index;

            byte literalLength = source[_compressedBuffer.Index];
            byte copyLength = source[_compressedBuffer.Index + 1];

            // If token not fully in compressed buffer, stop
            int compressedLength = 2 + literalLength + (copyLength > 0 ? 2 : 0);
            if (_compressedBuffer.Length < compressedLength)
            {
                return false;
            }

            // Read literal bytes
            Helpers.ArrayCopy(source, _compressedBuffer.Index + 2, array, index, literalLength);
            index += literalLength;
            _compressedBuffer.Index += 2 + literalLength;

            // Read copied bytes
            if (copyLength > 0)
            {
                ushort copyFromOffset = (ushort)(source[_compressedBuffer.Index] + (source[_compressedBuffer.Index + 1] << 8));
                _compressedBuffer.Index += 2;

                Helpers.ArrayCopy(source, tokenStart - copyFromOffset, array, index, copyLength);
                index += copyLength;
            }

            if (copyLength + literalLength == 0)
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
            if (_compressedBuffer.Length < Lz4Constants.Preamble.Length) { return false; }

            for (int i = 0; i < Lz4Constants.Preamble.Length; ++i)
            {
                if (_compressedBuffer.Array[i] != Lz4Constants.Preamble[i])
                {
                    return false;
                }
            }

            _compressedBuffer.Index += Lz4Constants.Preamble.Length;
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
