// GPL-3.0-or-later. Read-only view over uncompressed, unencrypted RAR5 PKG volumes.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using LibOrbisPkg.Util;

namespace Preparador
{
    public sealed class StoredRar5 : Stream
    {
        sealed class Part
        {
            public string Path;
            public long Offset, Size, Start;
            public FileStream File;
            public byte[] Memory;
        }

        readonly List<Part> parts = new List<Part>();
        long position, length;
        public static uint Crc(byte[] bytes)
        {
            uint crc = 0xffffffff;
            foreach (byte b in bytes)
            {
                crc ^= b;
                for (int n = 0; n < 8; n++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
            }

            return ~crc;
        }

        static ulong Var(Stream input, List<byte> record = null)
        {
            ulong value = 0;
            int shift = 0;
            while (shift < 64)
            {
                int b = input.ReadByte();
                if (b < 0)
                    throw new EndOfStreamException();
                record?.Add((byte)b);
                value |= (ulong)(b & 127) << shift;
                if ((b & 128) == 0)
                    return value;
                shift += 7;
            }

            throw new InvalidDataException(L.T("RAR5: inteiro inválido."));
        }

        static byte[] ReadExact(Stream input, int count)
        {
            byte[] b = new byte[count];
            int done = 0;
            while (done < count)
            {
                int n = input.Read(b, done, count - done);
                if (n == 0)
                    throw new EndOfStreamException();
                done += n;
            }

            return b;
        }

        public static StoredRar5 TryOpen(string first)
        {
            if (!Path.GetExtension(first).Equals(".rar", StringComparison.OrdinalIgnoreCase))
                return null;
            string[] paths;
            var match = Regex.Match(Path.GetFileName(first), @"^(.*\.part)(\d+)\.rar$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var candidates = Directory.GetFiles(Path.GetDirectoryName(first)).Select(p => new { Path = p, M = Regex.Match(Path.GetFileName(p), "^" + Regex.Escape(match.Groups[1].Value) + @"(\d+)\.rar$", RegexOptions.IgnoreCase) }).Where(x => x.M.Success).OrderBy(x => int.Parse(x.M.Groups[1].Value)).ToArray();
                for (int i = 0; i < candidates.Length; i++)
                    if (int.Parse(candidates[i].M.Groups[1].Value) != i + 1)
                        throw new InvalidDataException(L.T("RAR: falta um volume intermediário."));
                paths = candidates.Select(x => x.Path).ToArray();
            }
            else
                paths = new[]
                {
                    first
                };
            var result = new StoredRar5();
            string entryName = null;
            try
            {
                for (int index = 0; index < paths.Length; index++)
                {
                    using (var file = File.OpenRead(paths[index]))
                    {
                        if (!ReadExact(file, 8).SequenceEqual(new byte[] { 82, 97, 114, 33, 26, 7, 1, 0 }))
                        {
                            result.Dispose();
                            return null;
                        }

                        int files = 0;
                        bool found = false;
                        while (file.Position < file.Length)
                        {
                            uint expected = BitConverter.ToUInt32(ReadExact(file, 4), 0);
                            var bytes = new List<byte>();
                            ulong headerSize = Var(file, bytes);
                            if (headerSize > 1024 * 1024)
                                throw new InvalidDataException(L.T("Cabeçalho RAR5 excessivo."));
                            byte[] body = ReadExact(file, (int)headerSize);
                            bytes.AddRange(body);
                            if (Crc(bytes.ToArray()) != expected)
                                throw new InvalidDataException(L.T("CRC de cabeçalho RAR5 incorreto."));
                            long dataOffset = file.Position;
                            using (var header = new MemoryStream(body))
                            {
                                ulong type = Var(header), flags = Var(header), extra = (flags & 1) != 0 ? Var(header) : 0, dataSize = (flags & 2) != 0 ? Var(header) : 0;
                                if (type == 4)
                                {
                                    result.Dispose();
                                    return null;
                                }

                                if (type == 5)
                                    break;
                                if (type == 2)
                                {
                                    ulong fileFlags = Var(header);
                                    Var(header);
                                    Var(header);
                                    if ((fileFlags & 2) != 0)
                                        ReadExact(header, 4);
                                    if ((fileFlags & 4) != 0)
                                        ReadExact(header, 4);
                                    ulong compression = Var(header);
                                    Var(header);
                                    ulong nameLength = Var(header);
                                    if (nameLength > 65536)
                                        throw new InvalidDataException(L.T("Nome RAR5 excessivo."));
                                    string name = new UTF8Encoding(false, true).GetString(ReadExact(header, (int)nameLength));
                                    if ((fileFlags & 1) == 0)
                                    {
                                        files++;
                                        if (files > 1 || !name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase) || ((compression >> 7) & 7) != 0)
                                        {
                                            result.Dispose();
                                            return null;
                                        }

                                        if (entryName != null && entryName != name)
                                            throw new InvalidDataException(L.T("Volumes RAR pertencem a arquivos diferentes."));
                                        entryName = name;
                                        if (((flags & 8) != 0) != (index > 0) || ((flags & 16) != 0) != (index < paths.Length - 1))
                                            throw new InvalidDataException(L.T("Conjunto RAR incompleto ou sequência inválida."));
                                        long extraStart = body.Length - checked((long)extra);
                                        if (extraStart < header.Position)
                                            throw new InvalidDataException(L.T("RAR5: extras inválidos."));
                                        header.Position = extraStart;
                                        while (header.Position < header.Length)
                                        {
                                            ulong recordSize = Var(header);
                                            long end = checked(header.Position + (long)recordSize);
                                            if (end > header.Length)
                                                throw new InvalidDataException(L.T("RAR5: extra truncado."));
                                            ulong recordType = Var(header);
                                            if (recordType == 1 || recordType == 5)
                                            {
                                                result.Dispose();
                                                return null;
                                            }

                                            header.Position = end;
                                        }

                                        long size = checked((long)dataSize);
                                        if (size <= 0 || dataOffset + size > file.Length)
                                            throw new InvalidDataException(L.T("Volume RAR incompleto."));
                                        result.parts.Add(new Part { Path = paths[index], Offset = dataOffset, Size = size, Start = result.length });
                                        result.length = checked(result.length + size);
                                        found = true;
                                    }
                                }

                                long next = checked(dataOffset + (long)dataSize);
                                if (next > file.Length)
                                    throw new InvalidDataException(L.T("RAR5 truncado."));
                                file.Position = next;
                            }
                        }

                        if (!found)
                        {
                            result.Dispose();
                            return null;
                        }
                    }
                }

                foreach (var part in result.parts)
                    part.File = new FileStream(part.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.RandomAccess);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        public string BufferLastSmallVolume()
        {
            var p = parts.Last();
            if (parts.Count < 2 || p.Size > 1280L * 1024 * 1024)
                return null;
            p.Memory = new byte[checked((int)p.Size)];
            long read = 0;
            while (read < p.Size)
            {
                int n = ReadAt(p.Start + read, p.Memory, (int)read, (int)Math.Min(4 * 1024 * 1024, p.Size - read), false);
                if (n <= 0)
                    throw new EndOfStreamException();
                read += n;
            }

            p.File.Dispose();
            p.File = null;
            return p.Path;
        }

        public int ReadAt(long pos, byte[] buffer, int offset, int count, bool useMemory = true)
        {
            int done = 0;
            while (done < count && pos < length)
            {
                var p = parts.Last(x => x.Start <= pos);
                long into = pos - p.Start;
                int n = (int)Math.Min(count - done, p.Size - into);
                int got;
                if (useMemory && p.Memory != null)
                {
                    Buffer.BlockCopy(p.Memory, (int)into, buffer, offset + done, n);
                    got = n;
                }
                else
                {
                    lock (p.File)
                    {
                        p.File.Position = p.Offset + into;
                        got = p.File.Read(buffer, offset + done, n);
                    }
                }

                if (got <= 0)
                    throw new EndOfStreamException();
                done += got;
                pos += got;
            }

            return done;
        }

        public override int Read(byte[] b, int off, int n)
        {
            int got = ReadAt(position, b, off, n);
            position += got;
            return got;
        }

        public override long Seek(long off, SeekOrigin origin)
        {
            long next = origin == SeekOrigin.Begin ? off : origin == SeekOrigin.Current ? position + off : length + off;
            if (next < 0)
                throw new IOException(L.T("Seek negativo."));
            position = next;
            return position;
        }

        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override long Length => length;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override void Flush()
        {
        }

        public override void SetLength(long n)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] b, int o, int n)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var p in parts)
                {
                    p.File?.Dispose();
                    p.Memory = null;
                }

            base.Dispose(disposing);
        }
    }

    public sealed class PkgStreamMemory : IMemoryReader
    {
        readonly Stream stream;
        readonly long start;
        public PkgStreamMemory(Stream stream, long start)
        {
            this.stream = stream;
            this.start = start;
        }

        public void Read(long pos, byte[] bytes, int offset, int count)
        {
            var rar = stream as StoredRar5;
            if (rar != null)
            {
                if (rar.ReadAt(start + pos, bytes, offset, count) != count)
                    throw new EndOfStreamException();
                return;
            }

            lock (stream)
            {
                stream.Position = start + pos;
                int done = 0;
                while (done < count)
                {
                    int n = stream.Read(bytes, offset + done, count - done);
                    if (n == 0)
                        throw new EndOfStreamException();
                    done += n;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}