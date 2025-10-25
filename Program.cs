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

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Formats.Tar;
using System.Security.Cryptography;

using BelNytheraSeiche.WaveletMatrix;

namespace BelNytheraSeiche.FMArchive;

/// <summary>
/// FM Archive: A command-line utility for creating and expanding custom, encrypted,
/// and text-encoded archives.
///
/// This tool packages specified files and directories into a TAR archive,
/// splits it into chunks, applies text encoding (Base64, z85, etc.),
/// compresses each chunk using FM-Index, and encrypts it using AES-GCM.
/// The resulting text chunks are stored in a standard ZIP file for portability
/// and to bypass email filters.
///
/// ---
///
/// ### Usage:
///
/// #### 1. Show Info
/// `> fmarchive [-i | --info]`
/// (Displays version information)
///
/// #### 2. Show Help
/// `> fmarchive [-h | --help | -?]`
/// (Displays this help message)
///
/// #### 3. Create Archive
/// `> fmarchive [c | create] PATHNAME [...]`
/// (Creates an archive from the specified files/directories)
/// `> fmarchive PATHNAME [...]`
/// (Default action: Creates an archive if paths are not ZIP files)
///
/// #### 4. Expand Archive
/// `> fmarchive [x | expand] PATHNAME.ZIP [...]`
/// (Expands one or more archives)
/// `> fmarchive PATHNAME.ZIP [...]`
/// (Default action: Expands the archive if paths are ZIP files)
///
/// ---
///
/// ### Configuration (config.json):
///
/// The behavior is controlled by a `config.json` file in the application directory:
/// - `default_name`: Default filename for the created ZIP archive.
/// - `cryptography_key`: The secret key used for AES-GCM encryption.
/// - `compression_level`: ZIP compression level (0=Optimal, 1=Fastest, 2=NoCompression, 3=SmallestSize).
/// - `text_encoder`: The text encoding to use ("base32", "base64", "base122", "z85").
///
/// ```json
/// {
///   "default_name": "archive",
///   "cryptography_key": "john doe",
///   "compression_level": 0,
///   "text_encoder": "base64",
///   "comments": [
///     "compression_level: 0 (recommended, faster)",
///     "text_encoder: base64 (recommended, smaller)"
///   ]
/// }
/// ```
/// 
/// </summary>
class Program
{
#if DEBUG
    static string BaseDirectory { get; } = Environment.CurrentDirectory;
#else
    static string BaseDirectory { get; } = AppContext.BaseDirectory;
#endif
    static readonly EnumerationOptions enumerationOptions_ = new();

    // 
    // 

    static void Main(string[] args)
    {
        try
        {
            var pathJson = Path.Combine(BaseDirectory, "config.json");
            if (!File.Exists(pathJson))
                throw new Exception($"config.json was not found in the application directory ({BaseDirectory}).");

            using var configJson = JsonDocument.Parse(File.ReadAllText(pathJson));
            var nameDefault = configJson.RootElement.GetProperty("default_name").GetString() ?? "archive";
            var keyCryptography = SHA256.HashData(Encoding.UTF8.GetBytes(configJson.RootElement.GetProperty("cryptography_key").GetString() ?? "archive"));
            var compressionLevel = configJson.RootElement.GetProperty("compression_level").GetInt32() switch
            {
                0 => CompressionLevel.Optimal,
                1 => CompressionLevel.Fastest,
                2 => CompressionLevel.NoCompression,
                3 => CompressionLevel.SmallestSize,
                _ => throw new Exception("Unknown compression level."),
            };
            var textEncoder = configJson.RootElement.GetProperty("text_encoder").GetString() switch
            {
                "base32" => "b32",
                "base64" => "b64",
                "base122" => "b122",
                "z85" => "z85",
                _ => throw new Exception("Unknown text encoder."),
            };

            switch (args)
            {
                case [] or ["-i" or "--info" or "/i" or "/info"]:
                    __ShowInfo();
                    break;
                case ["-h" or "--help" or "-?" or "/h" or "/help" or "/?"]:
                    __ShowHelp();
                    break;
                case ["c" or "create", ..]:
                    if (args.Length == 1 || !args[1..].All(n => Path.Exists(n)))
                        throw new Exception("Arguments contains no pathname or invalid pathname.");
                    RunCreate(keyCryptography, compressionLevel, textEncoder, Path.Combine(BaseDirectory, nameDefault + ".zip"), args[1..]);
                    break;
                case ["x" or "expand", ..]:
                    if (args.Length == 1 || !args[1..].All(n => Path.Exists(n)))
                        throw new Exception("Arguments contains no pathname or invalid pathname.");
                    __Expand(keyCryptography, textEncoder, args[1..]);
                    break;
                default:
                    if (!args.All(n => Path.Exists(n)))
                        throw new Exception("Arguments contains invalid pathname.");

                    var extension1 = Path.GetExtension(args[0])?.ToLowerInvariant();
                    if (extension1 is ".zip")
                        __Expand(keyCryptography, textEncoder, args);
                    else
                        RunCreate(keyCryptography, compressionLevel, textEncoder, Path.Combine(BaseDirectory, nameDefault + ".zip"), args);
                    break;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[ERROR]");
            Console.Error.WriteLine(exception.Message);
        }

        #region @@
        static void __ShowInfo()
        {
            Console.WriteLine($"[FM Archive]");
            Console.WriteLine($"VERSION: {Environment.Version}");
            Console.WriteLine();
        }
        static void __ShowHelp()
        {
            Console.WriteLine($"[FM Archive]");
            Console.WriteLine($"VERSION: {Environment.Version}");
            Console.WriteLine("-");
            Console.WriteLine("# SHOW INFO");
            Console.WriteLine("> fmarchive [-i | --info | /i | /info]");
            Console.WriteLine();
            Console.WriteLine("# SHOW HELP");
            Console.WriteLine("> fmarchive [-h | --help | -? | /h | /help | /?]");
            Console.WriteLine();
            Console.WriteLine("# CREATE ARCHIVE");
            Console.WriteLine("> fmarchive PATHNAME[...]");
            Console.WriteLine("> fmarchive [c | create] PATHNAME[...]");
            Console.WriteLine();
            Console.WriteLine("# EXPAND ARCHIVE");
            Console.WriteLine("> fmarchive PATHNAME.ZIP[...]");
            Console.WriteLine("> fmarchive [x | expand] PATHNAME.ZIP[...]");
            Console.WriteLine();
        }
        static void __Expand(byte[] keyCryptography, string textDecoder, string[] args)
        {
            foreach (var path in args)
            {
                var fullpath = Path.GetFullPath(path);

                var extension = Path.GetExtension(fullpath)?.ToLowerInvariant();
                if (extension is not ".zip")
                    Console.WriteLine($"SKIP (NOT ARCHIVE): {fullpath}");
                else
                {
                    var directory = Path.Combine(Path.GetDirectoryName(fullpath)!, Path.GetFileNameWithoutExtension(fullpath));
                    if (directory is null or "")
                        Console.WriteLine($"SKIP (NO FILENAME): {fullpath}");
                    else if (Directory.Exists(directory))
                        Console.WriteLine($"SKIP (DIRECTORY EXISTS): {fullpath}");
                    else
                    {
                        Directory.CreateDirectory(directory);
                        RunExpand(keyCryptography, textDecoder, directory, fullpath);
                    }
                }
            }
        }
        #endregion
    }

    static void RunCreate(byte[] keyCryptography, CompressionLevel compressionLevel, string textEncoder, string pathArchive, string[] pathEntries)
    {
        var fileTar = Path.GetTempFileName();
        var successful = false;
        try
        {
            Func<ReadOnlySpan<byte>, string> textEncode = textEncoder switch { "b32" => __B32, "b64" => __B64, "b122" => __B122, "z85" => __Z85, _ => __B64 };

            using var fileTarStream = new FileStream(fileTar, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            {
                using var tarWriter = new TarWriter(fileTarStream, true);
                var (files, directories) = pathEntries.Select(n => n.Replace('/', '\\').TrimEnd('\\')).Aggregate((new List<FileInfo>(), new List<DirectoryInfo>()), (acc, current) =>
                {
                    var fullpath = Path.GetFullPath(current);
                    if (Directory.Exists(fullpath))
                        acc.Item2.Add(new DirectoryInfo(fullpath));
                    else if (File.Exists(fullpath))
                        acc.Item1.Add(new FileInfo(fullpath));

                    return acc;
                });
                foreach (var file in files.DistinctBy(n => n.FullName, StringComparer.OrdinalIgnoreCase).Where(n => FileAttributes.None == (n.Attributes & (FileAttributes.Hidden | FileAttributes.System))))
                {
                    Console.WriteLine($"ADDING: {file.Name}");
                    tarWriter.WriteEntry(file.FullName, file.Name);
                }
                foreach (var directory in directories.DistinctBy(n => n.FullName, StringComparer.OrdinalIgnoreCase).Where(n => FileAttributes.None == (n.Attributes & (FileAttributes.Hidden | FileAttributes.System))))
                {
                    __WriteDirectoryRecursive(tarWriter, "", directory);
                }
            }

            Console.WriteLine($"FULL LENGTH: {fileTarStream.Length}");
            fileTarStream.Seek(0, SeekOrigin.Begin);

            using var fileZipStream = new FileStream(pathArchive, FileMode.Create, FileAccess.Write, FileShare.None);
            {
                using var zipArchive = new ZipArchive(fileZipStream, ZipArchiveMode.Create, false, Encoding.UTF8);
                using var aes = new AesGcm(keyCryptography, 16);
                // max 256KB per chunk
                var index = 0;
                foreach (var buffer in __EnumerateReadBytes(fileTarStream, new byte[262144]))
                {
                    var bytes1 = FMIndex.Serialize(FMIndex.Create(textEncode(buffer.Span), 512), new() { CompressionLevel = compressionLevel });
                    var bytes2 = new byte[32 + bytes1.Length];
                    // var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];// 12 bytes
                    // var tag = new byte[16];
                    aes.Encrypt(bytes2.AsSpan(0, 12), bytes1, bytes2.AsSpan(32), bytes2.AsSpan(12, 16));
                    var encodedString = textEncode(bytes2);
                    var entry = zipArchive.CreateEntry($"{index:D08}.txt", compressionLevel);
                    using var entryWriter = new StreamWriter(entry.Open());
                    entryWriter.Write(encodedString);

                    index++;
                    Console.WriteLine($"CHUNK: {index}, LENGTH: {encodedString.Length}");
                }
            }

            successful = true;
            Console.WriteLine("SUCCESSFUL.");
        }
        finally
        {
            Task.Delay(1000).Wait();
            File.Delete(fileTar);
            if (!successful)
                File.Delete(pathArchive);
        }

        #region @@
        static string __B32(ReadOnlySpan<byte> data) => Base32Encoder.Encode(data);
        static string __B64(ReadOnlySpan<byte> data) => Convert.ToBase64String(data);
        static string __B122(ReadOnlySpan<byte> data) => Base122Encoder.Encode(data);
        static string __Z85(ReadOnlySpan<byte> data) => Z85Encoder.Encode(data);
        static IEnumerable<Memory<byte>> __EnumerateReadBytes(Stream stream, Memory<byte> buffer)
        {
            var restLength = stream.Length;
            while (restLength != 0)
            {
                var readLength = (int)(buffer.Length < restLength ? buffer.Length : restLength);
                stream.ReadExactly(buffer.Span[..readLength]);
                yield return buffer[..readLength];
                restLength -= readLength;
            }
        }
        static void __WriteDirectoryRecursive(TarWriter tarWriter, string basename, DirectoryInfo directory)
        {
            var name1 = basename + directory.Name + '/';
            Console.WriteLine($"ADDING: {name1}");
            tarWriter.WriteEntry(new PaxTarEntry(TarEntryType.Directory, name1) { ModificationTime = directory.LastWriteTimeUtc });

            foreach (var file in directory.EnumerateFiles("*", enumerationOptions_))
            {
                var name2 = name1 + file.Name;
                Console.WriteLine($"ADDING: {name2}");
                tarWriter.WriteEntry(file.FullName, name2);
            }

            foreach (var sub in directory.EnumerateDirectories("*", enumerationOptions_))
                __WriteDirectoryRecursive(tarWriter, name1, sub);
        }
        #endregion
    }

    static void RunExpand(byte[] keyCryptography, string textDecoder, string pathExpand, string pathArchive)
    {
        var fileTar = Path.GetTempFileName();
        try
        {
            Func<string, byte[]> textDecode = textDecoder switch { "b32" => __B32, "b64" => __B64, "b122" => __B122, "z85" => __Z85, _ => __B64 };

            using var fileTarStream = new FileStream(fileTar, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            {
                using var fileZipStream = new FileStream(pathArchive, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var zipArchive = new ZipArchive(fileZipStream, ZipArchiveMode.Read);
                using var aes = new AesGcm(keyCryptography, 16);
                var index = 0;
                foreach (var entry in zipArchive.Entries.OrderBy(n => n.Name).Where(n => Regex.IsMatch(n.Name, @"^[0-9]{8}\.txt$")))
                {
                    if ($"{index:D08}.txt" != entry.Name)
                        throw new Exception("Format is not compatible.");

                    using var entryReader = new StreamReader(entry.Open());
                    var bytes2 = textDecode(entryReader.ReadToEnd());
                    var bytes1 = new byte[bytes2.Length - 32];
                    aes.Decrypt(bytes2.AsSpan(0, 12), bytes2.AsSpan(32), bytes2.AsSpan(12, 16), bytes1);
                    var buffer = textDecode(FMIndex.Deserialize(bytes1).RestoreSourceText());
                    fileTarStream.Write(buffer);

                    index++;
                    Console.WriteLine($"CHUNK: {index}");
                }
            }

            fileTarStream.Seek(0, SeekOrigin.Begin);

            {
                using var tarReader = new TarReader(fileTarStream);
                TarEntry? tarEntry = null;
                while ((tarEntry = tarReader.GetNextEntry()) != null)
                {
                    if (tarEntry.EntryType == TarEntryType.Directory)
                    {
                        var directory = Path.Combine(pathExpand, tarEntry.Name.TrimEnd('/'));
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);
                        Console.WriteLine($"CREATE: {directory}");
                    }
                    else if (tarEntry.EntryType == TarEntryType.RegularFile)
                    {
                        using var memoryStream = new MemoryStream();
                        tarEntry.DataStream!.CopyTo(memoryStream);
                        var fullpath = Path.Combine(pathExpand, tarEntry.Name);
                        File.WriteAllBytes(Path.Combine(pathExpand, tarEntry.Name), memoryStream.ToArray());
                        Console.WriteLine($"CREATE: {fullpath}");
                    }
                }
            }

            Console.WriteLine("SUCCESSFUL.");
        }
        finally
        {
            Task.Delay(1000).Wait();
            File.Delete(fileTar);
        }

        #region @@
        static byte[] __B32(string text) => Base32Encoder.Decode(text);
        static byte[] __B64(string text) => Convert.FromBase64String(text);
        static byte[] __B122(string text) => Base122Encoder.Decode(text);
        static byte[] __Z85(string text) => Z85Encoder.Decode(text);
        #endregion
    }
}
