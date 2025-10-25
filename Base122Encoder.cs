// The MIT License (MIT)
//
// Copyright (c) 2025 belnytheraseiche
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// This code is a C# port of the original JavaScript implementation of Base122.
// Original source: https://github.com/kevinAlbs/Base122/blob/master/base122.js
// 
// The original JavaScript code is licensed under the MIT License:
// 
// MIT License
// 
// Copyright (c) 2016 Kevin Albertson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Text;

namespace BelNytheraSeiche.FMArchive;

/// <summary>
/// Provides static methods for encoding binary data into a Base122-like, UTF-8 compatible string
/// and decoding it back into binary data.
/// This implementation works by processing data as a 7-bit stream,
/// escaping specific "illegal" 7-bit values (like NULL, LF, CR, quotes)
/// into 2-byte UTF-8 sequences.
/// </summary>
static class Base122Encoder
{
    static readonly byte[] Illeagals = [0, 0x0A, 0x0D, 0x22, 0x26, 0x5C];

    // 
    // 

    /// <summary>
    /// Encodes a read-only span of bytes into a Base122-like, UTF-8 compatible string.
    /// <br/>
    /// This implementation is a port of the original JavaScript library (kevinAlbs/Base122) and produces identical output.
    /// <br/>
    /// <strong>Warning:</strong> The resulting string may contain unescaped ASCII control characters
    /// (such as Tab) if they are not explicitly listed in the internal 'Illeagals' array to be escaped.
    /// </summary>
    /// <param name="data">The read-only span of bytes to encode.</param>
    /// <returns>A string containing the encoded data, represented as safe ASCII and 2-byte UTF-8 escape sequences.</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var encoded = new List<byte>(data.Length + data.Length / 3 + 1);
        (int, int) state = (0, 0);
        while (__TryFetch7Bits(data, out var bits1, ref state))
        {
            var illegalIndex = Array.IndexOf(Illeagals, bits1);
            if (illegalIndex == -1)
                encoded.Add(bits1);
            else
            {
                var (b1, b2) = (0b11000010u, 0b10000000u);
                var fetched = __TryFetch7Bits(data, out var bits2, ref state);
                if (fetched)
                    b1 |= (0b111u & (uint)illegalIndex) << 2;
                else
                {
                    b1 |= 0b11100u;
                    bits2 = bits1;
                }

                var firstBit = (bits2 & 0b01000000u) != 0 ? 1 : 0;
                b1 |= (bits2 & 0b01000000u) != 0 ? 1u : 0u;
                b2 |= bits2 & 0b00111111u;
                encoded.Add((byte)b1);
                encoded.Add((byte)b2);
            }
        }

        return Encoding.UTF8.GetString(encoded.ToArray());

        #region @@
        static bool __TryFetch7Bits(ReadOnlySpan<byte> data, out byte bits, ref (int, int) state)
        {
            var (index, currentBit) = state;
            if (index == data.Length)
            {
                bits = 0;
                return false;
            }

            var firstByte = data[index];
            var firstPart = (byte)((((0b11111110u >> currentBit) & firstByte) << currentBit) >> 1);
            currentBit += 7;
            if (currentBit < 8)
            {
                (bits, state) = (firstPart, (index, currentBit));
                return true;
            }

            currentBit -= 8;
            index++;
            if (index == data.Length)
            {
                (bits, state) = (firstPart, (index, currentBit));
                return true;
            }

            var secondByte = data[index];
            var secondPart = (byte)(((0xFF00u >> currentBit) & secondByte & 0x00FFu) >> (8 - currentBit));
            (bits, state) = ((byte)(firstPart | secondPart), (index, currentBit));
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Decodes a Base122-like encoded string (as a read-only span of characters) back into its original byte array.
    /// <br/>
    /// This implementation is a port of the original JavaScript library (kevinAlbs/Base122) and produces identical output.
    /// <br/>
    /// <strong>Warning:</strong> The algorithm does not perform robust error detection.
    /// Malformed input may be decoded without exceptions, resulting in corrupted output.
    /// </summary>
    /// <param name="text">The read-only span of characters (UTF-16) to decode.</param>
    /// <returns>A new byte array containing the decoded binary data.</returns>
    public static byte[] Decode(ReadOnlySpan<char> text)
    {
        var decoded = new List<byte>(text.Length + text.Length / 3 + 1);
        (int, int) state = (0, 0);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if ((c & 0xFF80u) == 0)
                __Push7Bits(decoded, (byte)c, ref state);
            else
            {
                var illegalIndex = (c >> 8) & 7;
                if (illegalIndex != 7)
                    __Push7Bits(decoded, Illeagals[illegalIndex], ref state);
                __Push7Bits(decoded, (byte)(c & 0x007Fu), ref state);
            }
        }

        return decoded.ToArray();

        #region @@
        static void __Push7Bits(List<byte> decoded, byte value, ref (int, int) state)
        {
            var (bitOfByte, currentByte) = state;
            value <<= 1;
            currentByte |= value >> bitOfByte;
            bitOfByte += 7;
            if (bitOfByte >= 8)
            {
                decoded.Add((byte)currentByte);
                bitOfByte -= 8;
                currentByte = (value << (7 - bitOfByte)) & 0x00FF;
            }
            state = (bitOfByte, currentByte);
        }
        #endregion
    }
}
