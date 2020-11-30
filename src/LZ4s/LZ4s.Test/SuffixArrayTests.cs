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
            SortAndVerify(File.ReadAllBytes(@"Content\Example.json"), write: true);
        }

        [Fact]
        public void SuffixArray_SortIdenticalBytes()
        {
            // Identical bytes are the worst case for suffix sorting,
            // since every suffix is identical for the longest possible
            // length.
            SortAndVerify(new byte[8192]);
        }

        private void SortAndVerify(Span<byte> text, bool write = false)
        {
            SuffixArrayBuilder builder = new SuffixArrayBuilder(text.Length);
            ushort[] suffixArray = builder.SuffixArray(text);

            if (write)
            {
                //WriteSuffixArrayStrings(text, suffixArray);
                WriteNeighbors(text, suffixArray);
            }

            VerifySuffixArray(text, suffixArray);
        }

        private void WriteNeighbors(Span<byte> text, ushort[] suffixArray)
        {
            Span<byte> previous = text.Slice(suffixArray[0]);

            Span<byte> current = text.Slice(suffixArray[1]);
            int previousLength = MatchingLength(previous, current);

            _output.WriteLine($"{suffixArray[0]:n0}\t{previousLength}\t{Encoding.UTF8.GetString(previous.First(previousLength))}");

            int currentLength = 0;
            for (int i = 1; i < text.Length - 1; ++i)
            {
                Span<byte> next = text.Slice(suffixArray[i + 1]);
                currentLength = MatchingLength(current, next);

                _output.WriteLine($"{suffixArray[i]:n0}\t{currentLength}\t{Encoding.UTF8.GetString(current.First(Math.Max(previousLength, currentLength)))}");

                previous = current;
                previousLength = currentLength;
                current = next;
            }

            _output.WriteLine($"{suffixArray[text.Length - 1]:n0}\t{currentLength}\t{Encoding.UTF8.GetString(current.First(previousLength))}");

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
                Assert.True(ComparesBefore(previous, current), $"At order {i:n0}, failed:\r\n{suffixArray[i]:n0}\t{Encoding.UTF8.GetString(current.First(255))}\r\nPrevious:\t{Encoding.UTF8.GetString(previous.First(255))}");

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

        private int MatchingLength(Span<byte> left, Span<byte> right)
        {
            int commonLength = Math.Min(left.Length, right.Length);
            if (commonLength > 256) { commonLength = 256; }

            int i;
            for (i = 0; i < commonLength; ++i)
            {
                if (left[i] != right[i]) { return i; }
            }

            return i - 1;
        }
    }

    internal static class SpanExtensions
    {
        public static Span<T> First<T>(this Span<T> span, int length)
        {
            return (span.Length <= length ? span : span.Slice(0, length));
        }
    }
}
