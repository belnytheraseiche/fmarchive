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
/// Provides static methods for encoding binary data into Base32 text representation
/// and decoding Base32 text back into binary data, according to the RFC 4648 standard.
/// </summary>
static class Base32Encoder
{
    static readonly char[] E = [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '2', '3', '4', '5', '6', '7'
    ];
    static readonly byte[] D = [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
        0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
        0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
    ];

    // 
    // 

    /// <summary>
    /// Encodes a read-only span of bytes into its RFC 4648 Base32 string representation, including padding.
    /// </summary>
    /// <param name="data">The read-only span of bytes to encode.</param>
    /// <returns>The Base32 encoded string.</returns>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var fullLength = (data.Length + 4) / 5 * 8;
        var encoded = new char[fullLength];

        var (offset1, offset2) = (0, 0);
        while (offset1 + 5 <= data.Length)
        {
            var (value1, value2, value3, value4, value5) = ((uint)data[offset1], (uint)data[offset1 + 1], (uint)data[offset1 + 2], (uint)data[offset1 + 3], (uint)data[offset1 + 4]);
            encoded[offset2] = E[value1 >> 3];
            encoded[offset2 + 1] = E[((value1 & 0x07u) << 2) | (value2 >> 6)];
            encoded[offset2 + 2] = E[(value2 & 0x3Eu) >> 1];
            encoded[offset2 + 3] = E[((value2 & 0x01u) << 4) | (value3 >> 4)];
            encoded[offset2 + 4] = E[((value3 & 0x0Fu) << 1) | (value4 >> 7)];
            encoded[offset2 + 5] = E[(value4 & 0x7Cu) >> 2];
            encoded[offset2 + 6] = E[((value4 & 0x03u) << 3) | (value5 >> 5)];
            encoded[offset2 + 7] = E[value5 & 0x1Fu];
            offset1 += 5;
            offset2 += 8;
        }

        var remaining = data.Length - offset1;
        switch (remaining)
        {
            case 1:
                {
                    var value1 = data[offset1];
                    encoded[offset2] = E[value1 >> 3];
                    encoded[offset2 + 1] = E[(value1 & 0x07u) << 2];
                    Array.Fill(encoded, '=', offset2 + 2, 6);
                }
                break;
            case 2:
                {
                    var (value1, value2) = ((uint)data[offset1], (uint)data[offset1 + 1]);
                    encoded[offset2] = E[value1 >> 3];
                    encoded[offset2 + 1] = E[((value1 & 0x07u) << 2) | (value2 >> 6)];
                    encoded[offset2 + 2] = E[(value2 & 0x3Eu) >> 1];
                    encoded[offset2 + 3] = E[(value2 & 0x01u) << 4];
                    Array.Fill(encoded, '=', offset2 + 4, 4);
                }
                break;
            case 3:
                {
                    var (value1, value2, value3) = ((uint)data[offset1], (uint)data[offset1 + 1], (uint)data[offset1 + 2]);
                    encoded[offset2] = E[value1 >> 3];
                    encoded[offset2 + 1] = E[((value1 & 0x07u) << 2) | (value2 >> 6)];
                    encoded[offset2 + 2] = E[(value2 & 0x3Eu) >> 1];
                    encoded[offset2 + 3] = E[((value2 & 0x01u) << 4) | (value3 >> 4)];
                    encoded[offset2 + 4] = E[(value3 & 0x0Fu) << 1];
                    Array.Fill(encoded, '=', offset2 + 5, 3);
                }
                break;
            case 4:
                {
                    var (value1, value2, value3, value4) = ((uint)data[offset1], (uint)data[offset1 + 1], (uint)data[offset1 + 2], (uint)data[offset1 + 3]);
                    encoded[offset2] = E[value1 >> 3];
                    encoded[offset2 + 1] = E[((value1 & 0x07u) << 2) | (value2 >> 6)];
                    encoded[offset2 + 2] = E[(value2 & 0x3Eu) >> 1];
                    encoded[offset2 + 3] = E[((value2 & 0x01u) << 4) | (value3 >> 4)];
                    encoded[offset2 + 4] = E[((value3 & 0x0Fu) << 1) | (value4 >> 7)];
                    encoded[offset2 + 5] = E[(value4 & 0x7Cu) >> 2];
                    encoded[offset2 + 6] = E[(value4 & 0x03u) << 3];
                    encoded[offset2 + 7] = '=';
                }
                break;
        }

        return new(encoded);
    }

    /// <summary>
    /// Decodes an RFC 4648 Base32 encoded string (as a read-only span of characters) into its original byte array.
    /// Padding characters ('=') are automatically ignored.
    /// </summary>
    /// <param name="text">The read-only span of characters to decode. Tolerates lowercase letters.</param>
    /// <returns>A new byte array containing the decoded binary data.</returns>
    /// <exception cref="FormatException">Thrown if the input text contains invalid Base32 characters or has an invalid (non-canonical) length after padding removal.</exception>
    public static byte[] Decode(ReadOnlySpan<char> text)
    {
        var trimmed = text.TrimEnd('=');
        if (trimmed.Length % 8 is 1 or 3 or 6)
            throw new FormatException("Invalid text.");

        var fullLength = trimmed.Length * 5 / 8;
        var decoded = new byte[fullLength];

        var (offset1, offset2) = (0, 0);
        while (offset1 + 8 <= trimmed.Length)
        {
            var span = trimmed[offset1..(offset1 + 8)];
            var (value1, value2, value3, value4, value5, value6, value7, value8) =
                (D[span[0]], D[span[1]], D[span[2]], D[span[3]], D[span[4]], D[span[5]], D[span[6]], D[span[7]]);
            if (value1 == 0xFF || value2 == 0xFF || value3 == 0xFF || value4 == 0xFF || value5 == 0xFF || value6 == 0xFF || value7 == 0xFF || value8 == 0xFF)
                throw new FormatException("Invalid text.");

            decoded[offset2] = (byte)((value1 << 3) | (value2 >> 2));
            decoded[offset2 + 1] = (byte)((value2 << 6) | (value3 << 1) | (value4 >> 4));
            decoded[offset2 + 2] = (byte)((value4 << 4) | (value5 >> 1));
            decoded[offset2 + 3] = (byte)((value5 << 7) | (value6 << 2) | (value7 >> 3));
            decoded[offset2 + 4] = (byte)((value7 << 5) | value8);
            offset1 += 8;
            offset2 += 5;
        }

        var remaining = trimmed.Length - offset1;
        switch (remaining)
        {
            case 2:
                {
                    var span = trimmed[offset1..];
                    var (value1, value2) = (D[span[0]], D[span[1]]);
                    if (value1 == 0xFF || value2 == 0xFF)
                        throw new FormatException("Invalid text.");
                    decoded[offset2] = (byte)((value1 << 3) | (value2 >> 2));
                }
                break;
            case 4:
                {
                    var span = trimmed[offset1..];
                    var (value1, value2, value3, value4) = (D[span[0]], D[span[1]], D[span[2]], D[span[3]]);
                    if (value1 == 0xFF || value2 == 0xFF || value3 == 0xFF || value4 == 0xFF)
                        throw new FormatException("Invalid text.");
                    decoded[offset2] = (byte)((value1 << 3) | (value2 >> 2));
                    decoded[offset2 + 1] = (byte)((value2 << 6) | (value3 << 1) | (value4 >> 4));
                }
                break;
            case 5:
                {
                    var span = trimmed[offset1..];
                    var (value1, value2, value3, value4, value5) = (D[span[0]], D[span[1]], D[span[2]], D[span[3]], D[span[4]]);
                    if (value1 == 0xFF || value2 == 0xFF || value3 == 0xFF || value4 == 0xFF || value5 == 0xFF)
                        throw new FormatException("Invalid text.");
                    decoded[offset2] = (byte)((value1 << 3) | (value2 >> 2));
                    decoded[offset2 + 1] = (byte)((value2 << 6) | (value3 << 1) | (value4 >> 4));
                    decoded[offset2 + 2] = (byte)((value4 << 4) | (value5 >> 1));
                }
                break;
            case 7:
                {
                    var span = trimmed[offset1..];
                    var (value1, value2, value3, value4, value5, value6, value7) = (D[span[0]], D[span[1]], D[span[2]], D[span[3]], D[span[4]], D[span[5]], D[span[6]]);
                    if (value1 == 0xFF || value2 == 0xFF || value3 == 0xFF || value4 == 0xFF || value5 == 0xFF || value6 == 0xFF || value7 == 0xFF)
                        throw new FormatException("Invalid text.");
                    decoded[offset2] = (byte)((value1 << 3) | (value2 >> 2));
                    decoded[offset2 + 1] = (byte)((value2 << 6) | (value3 << 1) | (value4 >> 4));
                    decoded[offset2 + 2] = (byte)((value4 << 4) | (value5 >> 1));
                    decoded[offset2 + 3] = (byte)((value5 << 7) | (value6 << 2) | (value7 >> 3));
                }
                break;
        }

        return decoded;
    }
}
