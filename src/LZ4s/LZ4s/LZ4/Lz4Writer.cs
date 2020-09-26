using System;
using System.IO;
using System.Text;

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

#if DEBUG
            string literal = Encoding.UTF8.GetString(array, index, token.LiteralLength);
            string copy = Encoding.UTF8.GetString(array, index + token.LiteralLength, token.CopyLength);
            string copySource = Encoding.UTF8.GetString(array, index + token.LiteralLength - token.CopyFromRelativeIndex, token.CopyLength);
#endif

            _compressedBuffer.Append(token.LiteralLength);
            _compressedBuffer.Append(token.CopyLength);

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
            }
        }

        private void Close()
        {
            // Compress remaining pending content
            CompressBuffer();

            // Write the last bytes of the compressed buffer as a literal
            if (_uncompressedBuffer.Length > 0)
            {
                WriteToken(_uncompressedBuffer.Array, _uncompressedBuffer.Index, new Token(_uncompressedBuffer.Length, 0, 0));
            }

            // Zero token to indicate end of content
            WriteToken(_uncompressedBuffer.Array, 0, new Token(0, 0, 0));

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
