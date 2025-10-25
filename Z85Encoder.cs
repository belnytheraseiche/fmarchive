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

namespace BelNytheraSeiche.FMArchive;

/// <summary>
/// Provides static methods for encoding binary data into z85 text representation
/// and decoding z85 text back into binary data.
/// z85 is a Base85 encoding variant designed to be safe for use
/// in source code, JSON, XML, and other text-based formats.
/// </summary>
static class Z85Encoder
{
    static readonly char[] E = [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '.', '-', ':', '+', '=', '^', '!', '/', '*', '?', '&', '<', '>', '(', ')', '[', ']', '{', '}', '@', '%', '$', '#'
    ];
    static readonly byte[] D = [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0x44, 0xFF, 0x54, 0x53, 0x52, 0x48, 0xFF, 0x4B, 0x4C, 0x46, 0x41, 0xFF, 0x3F, 0x3E, 0x45,
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x40, 0xFF, 0x49, 0x42, 0x4A, 0x47,
        0x51, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32,
        0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x4D, 0xFF, 0x4E, 0x43, 0xFF,
        0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x4F, 0xFF, 0x50, 0xFF, 0xFF,
    ];
    static readonly uint[] P = [85u * 85u * 85u * 85u, 85u * 85u * 85u, 85u * 85u, 85u];

    // 
    // 

    /// <summary>
    /// Encodes a read-only span of bytes into its z85 string representation.
    /// </summary>
    /// <param name="data">The read-only span of bytes to encode.</param>
    /// <returns>The z85 encoded string.</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var fullLength = data.Length * 5 / 4 + (data.Length % 4 != 0 ? 1 : 0);
        var encoded = new char[fullLength];

        var (offset1, offset2) = (0, 0);
        while (offset1 + 4 <= data.Length)
        {
            var value = (uint)data[offset1] << 24 | (uint)data[offset1 + 1] << 16 | (uint)data[offset1 + 2] << 8 | data[offset1 + 3];
            var span = encoded.AsSpan(offset2, 5);
            span[0] = E[(value / P[0]) % 85];
            span[1] = E[(value / P[1]) % 85];
            span[2] = E[(value / P[2]) % 85];
            span[3] = E[(value / P[3]) % 85];
            span[4] = E[value % 85];
            offset1 += 4;
            offset2 += 5;
        }

        var remaining = data.Length - offset1;
        if (remaining != 0)
        {
            var value = 0u;
            for (var i = 0; i < remaining; i++)
                value |= (uint)data[offset1 + i] << (24 - 8 * i);
            for (var i = 0; i < remaining + 1; i++)
                encoded[offset2 + i] = E[(value / P[i]) % 85];
        }

        return new(encoded);
    }

    /// <summary>
    /// Decodes a z85 encoded string (as a read-only span of characters) into its original byte array.
    /// </summary>
    /// <param name="text">The read-only span of characters to decode.</param>
    /// <returns>A new byte array containing the decoded binary data.</returns>
    /// <exception cref="FormatException">Thrown if the input text contains invalid z85 characters.</exception>
    public static byte[] Decode(ReadOnlySpan<char> text)
    {
        var fullLength = text.Length * 4 / 5;
        var decoded = new byte[fullLength];

        var (offset1, offset2) = (0, 0);
        while (offset1 + 5 <= text.Length)
        {
            var value = 0u;
            for (var i = 0; i < 5; i++)
            {
                var c = text[offset1 + i];
                if ((c & 0xFF80) != 0 || D[c] == 0xFF)
                    throw new FormatException($"Contains invalid character (U+{c:X04}).");

                value = value * 85u + D[c];
            }

            var span = decoded.AsSpan(offset2, 4);
            span[0] = (byte)(value >> 24);
            span[1] = (byte)(value >> 16);
            span[2] = (byte)(value >> 8);
            span[3] = (byte)value;
            offset1 += 5;
            offset2 += 4;
        }

        var remaining = text.Length - offset1;
        if (remaining != 0)
        {
            var value = 0u;
            for (var i = 0; i < remaining; i++)
            {
                var c = text[offset1 + i];
                if ((c & 0xFF80) != 0 || D[c] == 0xFF)
                    throw new FormatException($"Contains invalid character (U+{c:X04}).");

                value = value * 85u + D[c];
            }
            for (var i = remaining; i < 5; i++)
                value = value * 85u + 84u;
            for (var i = 0; i < remaining - 1; i++)
                decoded[offset2 + i] = (byte)(value >> (24 - i * 8));
        }

        return decoded;
    }
}
