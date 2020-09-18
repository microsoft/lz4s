using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4sDictionaryTests
    {
        [Fact]
        public void Lz4sDictionary_Basics()
        {
            Lz4sDictionary dictionary = new Lz4sDictionary();
            byte[] data = File.ReadAllBytes("Content/Example.json");

            for (int i = 0; i + 3 < data.Length; ++i)
            {
                dictionary.Add(data, i, i);
            }

            List<long> results = new List<long>();

            for (int i = 0; i + 3 < data.Length; ++i)
            {
                dictionary.Matches(data, i, results);
                Assert.Contains(i, results);

                if (results.Count > 1)
                {
                    Debugger.Break();
                }
            }
        }

        [Fact]
        public void Lz4sDictionary2_Scan()
        {
            Lz4sDictionary2 dictionary = new Lz4sDictionary2();
            byte[] data = File.ReadAllBytes("Content/Example.json");

            int matchCount = 0;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < 1; ++i)
            {
                matchCount = dictionary.Scan(data, 0, data.Length, 0);
            }
            w.Stop();
        }
    }
}
