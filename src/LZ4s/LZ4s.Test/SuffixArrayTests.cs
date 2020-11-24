using LZ4s;
using System;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SuffixArray.Test
{
    public class SuffixArrayTests
    {
        private readonly ITestOutputHelper _output;

        public SuffixArrayTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SuffixArray_SortTinySample()
        {
            SortAndVerify(Encoding.UTF8.GetBytes("This is a sample."));
        }

        [Fact]
        public void SuffixArray_SortSampleJson()
        {
            SortAndVerify(File.ReadAllBytes(@"Content\Example.json"));
        }

        [Fact]
        public void SuffixArray_SortIdenticalBytes()
        {
            // Identical bytes are the worst case for suffix sorting,
            // since every suffix is identical for the longest possible
            // length.
            SortAndVerify(new byte[8192]);
        }

        private void SortAndVerify(Span<byte> text)
        {
            SuffixArrayBuilder builder = new SuffixArrayBuilder(text.Length);
            ushort[] suffixArray = builder.SuffixArray(text);
            VerifySuffixArray(text, suffixArray);
        }

        private void WriteSuffixArrayStrings(Span<byte> text, ushort[] suffixArray)
        {
            Span<byte> previous = text.Slice(suffixArray[0]);
            _output.WriteLine($"{suffixArray[0]:n0}\t{Encoding.UTF8.GetString(previous)}");

            for (int i = 1; i < text.Length; ++i)
            {
                Span<byte> current = text.Slice(suffixArray[i]);
                Assert.True(ComparesBefore(previous, current));

                _output.WriteLine($"{suffixArray[i]:n0}\t{Encoding.UTF8.GetString(current)}");
                previous = current;
            }
        }

        private void VerifySuffixArray(Span<byte> text, ushort[] suffixArray)
        {
            Span<byte> previous = text.Slice(suffixArray[0]);
            for (int i = 1; i < text.Length; ++i)
            {
                Span<byte> current = text.Slice(suffixArray[i]);
                Assert.True(ComparesBefore(previous, current), $"At order {i:n0}, failed:\r\n{suffixArray[i]:n0}\t{Encoding.UTF8.GetString(First(current, 255))}\r\nPrevious:\t{Encoding.UTF8.GetString(First(previous, 255))}");

                previous = current;
            }
        }

        private bool ComparesBefore(Span<byte> left, Span<byte> right)
        {
            int commonLength = Math.Min(left.Length, right.Length);
            if (commonLength > 256) { commonLength = 256; }

            for (int i = 0; i < commonLength; ++i)
            {
                if (left[i] < right[i])
                {
                    return true;
                }
                else if (left[i] > right[i])
                {
                    return false;
                }
            }

            if (commonLength == 256)
            {
                return true;
            }
            else
            {
                return left.Length <= right.Length;
            }
        }

        private Span<byte> First(Span<byte> span, int length)
        {
            return (span.Length <= length ? span : span.Slice(0, length));
        }
    }
}
