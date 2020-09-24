using System;
using System.IO;

namespace LZ4s
{
    public class Lz4Writer : IDisposable
    {
        private Stream _stream;
        private bool _closeStream;

        private Lz4sBuffer _uncompressedBuffer;
        private long _uncompressedBufferFilePosition;

        private Lz4sBuffer _compressedBuffer;
        private Lz4sDictionary _dictionary;

        public Lz4Writer(Stream stream, bool closeStream = true)
        {
            _stream = stream;
            _closeStream = closeStream;

            _uncompressedBuffer = new Lz4sBuffer();

            _compressedBuffer = new Lz4sBuffer();
            //_compressedBuffer.Append(Constants.Preamble, 0, Constants.Preamble.Length);

            _dictionary = new Lz4sDictionary();
        }

        public void Write(byte[] array, int index, int length)
        {
            int end = index + length;
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (length < 0 || end > array.Length) { throw new ArgumentOutOfRangeException(nameof(length)); }

            while (index < end)
            {
                // Fill uncompressed buffer from input
                index += _uncompressedBuffer.Read(array, index, end - index);

                // If uncompressed buffer full, compress, shift, clear
                if (index < end)
                {
                    CompressBuffer();
                }
            }
        }

        private void CompressBuffer()
        {
            while (_uncompressedBuffer.Length > Lz4Constants.MinimumCopyLength)
            {
               Token token = _dictionary.NextMatch(_uncompressedBuffer, _uncompressedBufferFilePosition);

                if (token.DecompressedLength > 0)
                {
                    WriteToken(_uncompressedBuffer.Array, _uncompressedBuffer.Index, token);
                    _uncompressedBuffer.Index += token.DecompressedLength;
                }
                else
                {
                    break;
                }
            }

            // Shift the uncompressed buffer to make space to read more
            _uncompressedBufferFilePosition += Math.Max(0, _uncompressedBuffer.Length - Lz4Constants.MaximumCopyFromDistance);
            _uncompressedBuffer.Shift(Lz4Constants.MaximumCopyFromDistance);
        }

        private void WriteToken(byte[] array, int index, Token token)
        {
            // Flush buffer if too full for token
            if (token.CompressedLength > _compressedBuffer.RemainingSpace)
            {
                _compressedBuffer.Write(_stream, Lz4Constants.MaximumCopyFromDistance);
                _compressedBuffer.Shift(Lz4Constants.MaximumCopyFromDistance);
            }

            int literalLengthLeft = token.LiteralLength;
            int copyLengthLeft = token.CopyLength;

            // Write a shared byte with literal and copy length parts
            WriteMarker(ref literalLengthLeft, ref copyLengthLeft);

            // Write remaining literal length
            WriteLength(literalLengthLeft, token.LiteralLength);

            // Write literal bytes
            if (token.LiteralLength > 0)
            {
                _compressedBuffer.Append(array, index, token.LiteralLength);
            }

            // If copy bytes...
            if (token.CopyLength > 0)
            {
                // Write copy relative position
                _compressedBuffer.Append((byte)token.CopyFromRelativeIndex);
                _compressedBuffer.Append((byte)(token.CopyFromRelativeIndex >> 8));

                // Write remaining copy length
                WriteLength(copyLengthLeft, token.CopyLength);
            }
        }

        private void WriteMarker(ref int literalLengthLeft, ref int copyLengthLeft)
        {
            int marker = 0;

            int markerLiteralLength = (literalLengthLeft >= 15 ? 15 : literalLengthLeft);
            marker += markerLiteralLength << 4;
            literalLengthLeft -= markerLiteralLength;

            int markerCopyLength = (copyLengthLeft >= 15 ? 15 : copyLengthLeft);
            marker += markerCopyLength;
            copyLengthLeft -= markerCopyLength;

            _compressedBuffer.Append((byte)marker);
        }

        private void WriteLength(int length, int originalLength)
        {
            // If marker wasn't maxed out, no additional bytes needed
            if (originalLength < 15) { return; }

            // Write any remaining maximum value bytes
            while (length >= 255)
            {
                _compressedBuffer.Append(255);
                length -= 255;
            }

            // Write final length byte
            _compressedBuffer.Append((byte)length);
        }

        private void Close()
        {
            // Zero token to indicate end of content
            WriteToken(null, 0, new Token(0, 0, 0));

            // Write everything
            _compressedBuffer.Write(_stream);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_stream != null)
            {
                Close();
                if (_closeStream) { _stream.Dispose(); }
                _stream = null;
            }
        }
    }
}
