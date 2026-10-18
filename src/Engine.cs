// Preparador local; GPL-3.0-or-later. LibOrbisPkg retains its LGPL-3.0 license.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using LibOrbisPkg.PFS;
using LibOrbisPkg.PKG;
using LibOrbisPkg.SFO;

namespace Preparador
{
    public sealed class Job
    {
        public string Base, Update, Destination, BasePassword, UpdatePassword;
        public string[] BaseFiles, UpdateFiles;
        public bool BufferLastRarVolume;
        public string UpdateReadyMarker;
        public string BufferedVolumeReleaseMarker;
    }

    public sealed class Engine
    {
        readonly Action<string> log;
        readonly Action<int, string> progress;
        readonly CancellationToken cancel;
        readonly string sevenZip;
        public Engine(Action<string> log, Action<int, string> progress, CancellationToken cancel, string sevenZip = null)
        {
            this.log = log;
            this.progress = progress;
            this.cancel = cancel;
            this.sevenZip = sevenZip ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "7z.exe");
        }

        static string Sfo(Pkg pkg, string key)
        {
            return pkg.ParamSfo?.ParamSfo?[key]?.ToString() ?? "";
        }

        public static string SafeTarget(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(":"))
                throw new InvalidDataException(L.T("Caminho inválido no arquivo: ") + relative);
            var bits = relative.Replace('\\', '/').Split('/');
            if (bits.Any(b => b == ".." || b == "." || b.EndsWith(" ") || b.EndsWith(".") || Regex.IsMatch(b, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase)))
                throw new InvalidDataException(L.T("Caminho inseguro no arquivo: ") + relative);
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Path.Combine(root, relative));
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(L.T("Caminho fora da pasta de trabalho."));
            return target;
        }

        public static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    slashes++;
                    continue;
                }

                result.Append('\\', c == '\"' ? slashes * 2 + 1 : slashes);
                result.Append(c);
                slashes = 0;
            }

            result.Append('\\', slashes * 2);
            return result.Append('"').ToString();
        }

        public static string FirstVolume(string input)
        {
            input = Path.GetFullPath(input);
            string dir = Path.GetDirectoryName(input), name = Path.GetFileName(input);
            var m = Regex.Match(name, @"^(.*\.part)(\d+)(\.rar)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var first = Directory.GetFiles(dir).FirstOrDefault(p => Regex.IsMatch(Path.GetFileName(p), "^" + Regex.Escape(m.Groups[1].Value) + @"0*1\.rar$", RegexOptions.IgnoreCase));
                if (first == null)
                    throw new FileNotFoundException(L.T("Falta a primeira parte RAR (part1). Mantenha todas as partes na mesma pasta."));
                return first;
            }

            m = Regex.Match(name, @"^(.*\.zip)\.\d{3}$", RegexOptions.IgnoreCase);
            if (m.Success)
                return Path.Combine(dir, m.Groups[1].Value + ".001");
            m = Regex.Match(name, @"^(.*)\.z\d{2}$", RegexOptions.IgnoreCase);
            if (m.Success)
                return Path.Combine(dir, m.Groups[1].Value + ".zip");
            m = Regex.Match(name, @"^(.*)\.r\d{2}$", RegexOptions.IgnoreCase);
            if (m.Success)
                return Path.Combine(dir, m.Groups[1].Value + ".rar");
            return input;
        }

        public static string[] ResolveSources(string fallback, string[] selection)
        {
            var selected = selection != null && selection.Length > 0 ? selection : new[]
            {
                fallback
            };
            foreach (string path in selected)
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    throw new FileNotFoundException(L.T("Parte não encontrada: ") + path);
            var first = selected.Select(FirstVolume).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => Regex.Replace(p, @"\d+", m => m.Value.PadLeft(12, '0')), StringComparer.OrdinalIgnoreCase).ToArray();
            if (first.Length > 1 && !first.All(p => Path.GetExtension(p).Equals(".zip", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException(L.T("Selecione partes do mesmo arquivo RAR/ZIP dividido. ZIPs independentes podem ser selecionados juntos; não misture PKGs ou conjuntos RAR diferentes."));
            foreach (var p in first)
                if (!File.Exists(p))
                    throw new FileNotFoundException(L.T("Falta a primeira parte: ") + p);
            return first;
        }

        void FreeSpace(string folder, long needed)
        {
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(folder)));
            if (drive.AvailableFreeSpace < needed + (1L << 30))
                throw new IOException(L.T("Espaço insuficiente. Esta etapa precisa de pelo menos ") + ((needed + (1L << 30)) / (double)(1L << 30)).ToString("F1") + L.T(" GB livres na unidade ") + drive.Name);
        }

        string Run7z(string command, string archive, string destination, string password, bool capture)
        {
            cancel.ThrowIfCancellationRequested();
            if (!File.Exists(sevenZip))
                throw new FileNotFoundException(L.T("Falta tools/7z.exe. Extraia o ZIP inteiro do aplicativo."));
            string args = command + " -y -bd -bsp0 -sccUTF-8 " + (capture ? "-slt -ba " : "-bb0 ") + Quote("-p" + (password ?? "")) + " " + (destination == null ? "" : Quote("-o" + destination) + " ") + "-- " + Quote(archive);
            var info = new ProcessStartInfo(sevenZip, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using (var process = Process.Start(info))
            {
                process.StandardInput.Close();
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                while (!process.WaitForExit(200))
                {
                    if (cancel.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        process.WaitForExit();
                        cancel.ThrowIfCancellationRequested();
                    }
                }

                string text = stdout.Result, errors = stderr.Result;
                if (process.ExitCode != 0)
                    throw new InvalidDataException(L.T("Não foi possível abrir/extrair o arquivo. Confira senha, partes ausentes ou download corrompido. 7-Zip: ") + (errors + "\n" + text).Substring(Math.Max(0, (errors + "\n" + text).Length - 1800)));
                return text;
            }
        }

        void ValidateArchive(string listing, string destination)
        {
            long total = 0;
            foreach (var line in listing.Replace("\r", "").Split('\n'))
            {
                if (line.StartsWith("Path = "))
                    SafeTarget(destination, line.Substring(7));
                if (line.StartsWith("Size = "))
                {
                    long n;
                    if (long.TryParse(line.Substring(7), out n))
                        total = checked(total + n);
                }

                if (line.StartsWith("Attributes = ") && Regex.IsMatch(line, @"(?:^|\s)l[rwxstST-]{9}(?:\s|$)"))
                    throw new InvalidDataException(L.T("Links simbólicos dentro de arquivos não são aceitos."));
                if ((line.StartsWith("Symbolic Link = ") || line.StartsWith("Hard Link = ")) && line.Split('=').Last().Trim().Length > 0)
                    throw new InvalidDataException(L.T("Links dentro de arquivos não são aceitos."));
            }

            FreeSpace(destination, total);
        }

        static IEnumerable<string> Files(string root)
        {
            foreach (string entry in Directory.GetFileSystemEntries(root))
            {
                var attr = File.GetAttributes(entry);
                if ((attr & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException(L.T("Link encontrado na pasta extraída."));
                if ((attr & FileAttributes.Directory) != 0)
                {
                    foreach (var f in Files(entry))
                        yield return f;
                }
                else
                    yield return entry;
            }
        }

        string UnpackInput(string source, string root, string password)
        {
            return UnpackInputs(ResolveSources(source, null), root, password);
        }

        string UnpackInputs(string[] sources, string root, string password)
        {
            if (sources.Length == 1 && Path.GetExtension(sources[0]).Equals(".pkg", StringComparison.OrdinalIgnoreCase))
                return sources[0];
            Directory.CreateDirectory(root);
            foreach (string source in sources)
            {
                cancel.ThrowIfCancellationRequested();
                log(L.T("Verificando volumes de ") + Path.GetFileName(source) + "...");
                ValidateArchive(Run7z("l", source, null, password, true), root);
                log(L.T("Descompactando ") + Path.GetFileName(source) + L.T(" (aguarde)..."));
                Run7z("x", source, root, password, false);
            }

            Files(root).Count();
            return root;
        }

        Pkg ReadPackage(string path)
        {
            using (var s = File.OpenRead(path))
                return new PkgReader(s).ReadPkg();
        }

        string ChoosePackage(string folder, bool update)
        {
            var packages = Files(folder).Where(p => p.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (packages.Length == 0)
                return null;
            var valid = new List<string>();
            foreach (var path in packages)
            {
                cancel.ThrowIfCancellationRequested();
                var pkg = ReadPackage(path);
                string category = Sfo(pkg, "CATEGORY").Trim('\0', ' ');
                if (Sfo(pkg, "TITLE_ID").Trim('\0', ' ') == "CUSA00223" && category == (update ? "gp" : "gd"))
                    valid.Add(path);
                else
                    log(L.T("Pacote adicional ignorado: ") + Path.GetFileName(path) + " (" + category + ")");
            }

            if (valid.Count != 1)
                throw new InvalidDataException(L.T("Esperado exatamente um PKG ") + (update ? L.T("de update") : L.T("base")) + L.T(" CUSA00223. Encontrados: ") + valid.Count + L.T(". Selecione o arquivo correto."));
            return valid[0];
        }

        void ExtractPackage(string path, string destination, bool update)
        {
            using (var input = File.OpenRead(path))
                ExtractPackage(input, destination, update);
        }

        void ExtractPackage(Stream input, string destination, bool update)
        {
            var pkg = new PkgReader(input).ReadPkg();
            string title = Sfo(pkg, "TITLE_ID").Trim('\0', ' '), version = Sfo(pkg, "APP_VER").Trim('\0', ' '), category = Sfo(pkg, "CATEGORY").Trim('\0', ' ');
            if (title != "CUSA00223" || category != (update ? "gp" : "gd"))
                throw new InvalidDataException(L.T("PKG incompatível: ") + title + " / " + category + L.T(". Use CUSA00223 e o tipo correto."));
            if (update && version != "01.07" && version != "1.07")
                throw new InvalidDataException(L.T("O update precisa ser 1.07. Detectado: ") + version);
            log(L.T("PKG confirmado: ") + title + L.T(" versão ") + version + L.T(". Extraindo PFS corrigido..."));
            var key = pkg.GetEkpfs();
            if (key == null)
                throw new InvalidDataException(L.T("Chave PFS indisponível. Este PKG não é compatível com a biblioteca."));
            {
                var outer = new PfsReader(new PkgStreamMemory(input, (long)pkg.Header.pfs_image_offset), pkg.Header.pfs_flags, key, null, null);
                var image = outer.GetFile("pfs_image.dat");
                if (image == null)
                    throw new InvalidDataException(L.T("pfs_image.dat ausente."));
                using (var view = image.GetView())
                using (var decompressed = new PFSCReader(view))
                {
                    var inner = new PfsReader(decompressed);
                    var files = inner.GetAllFiles().ToArray();
                    FreeSpace(destination, files.Sum(f => f.size));
                    var paths = files.Select(f => SafeTarget(destination, Regex.Replace(f.FullName, @"^/uroot/", "").TrimStart('/'))).ToArray();
                    var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var target in paths)
                        if (!duplicates.Add(target))
                            throw new InvalidDataException(L.T("Nomes duplicados no PKG."));
                    byte[] buffer = new byte[1024 * 1024];
                    long bytes = 0, total = files.Sum(f => f.size);
                    var timer = Stopwatch.StartNew();
                    for (int i = 0; i < files.Length; i++)
                    {
                        cancel.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(Path.GetDirectoryName(paths[i]));
                        // File views share their parent reader and must not dispose that parent between files.
                        var reader = files[i].GetView();
                        using (var output = File.Create(paths[i]))
                        {
                            long pos = 0;
                            while (pos < files[i].size)
                            {
                                cancel.ThrowIfCancellationRequested();
                                int n = (int)Math.Min(buffer.Length, files[i].size - pos);
                                reader.Read(pos, buffer, 0, n);
                                output.Write(buffer, 0, n);
                                pos += n;
                            }

                            if (output.Length != files[i].size)
                                throw new IOException(L.T("Tamanho extraído incorreto."));
                        }

                        bytes += files[i].size;
                        if (timer.ElapsedMilliseconds > 300 || i == files.Length - 1)
                        {
                            progress((int)(100 * bytes / Math.Max(1, total)), (update ? "Update" : L.T("Jogo")) + ": " + (i + 1) + "/" + files.Length + L.T(" arquivos"));
                            timer.Restart();
                        }
                    }
                }
            }

            var stream = input;
            foreach (var meta in pkg.Metas.Metas)
            {
                cancel.ThrowIfCancellationRequested();
                if ((int)meta.id < 0x1000 || !EntryNames.IdToName.ContainsKey(meta.id))
                    continue;
                string target = SafeTarget(destination, "sce_sys/" + EntryNames.IdToName[meta.id]);
                long size = meta.Encrypted ? ((long)meta.DataSize + 15) & ~15 : (long)meta.DataSize;
                if (size < 0 || size > 64 * 1024 * 1024)
                    throw new InvalidDataException(L.T("Metadado excessivamente grande."));
                byte[] data = new byte[(int)size];
                stream.Position = meta.DataOffset;
                int pos = 0;
                while (pos < data.Length)
                {
                    int n = stream.Read(data, pos, data.Length - pos);
                    if (n == 0)
                        throw new EndOfStreamException();
                    pos += n;
                }

                if (meta.Encrypted)
                    data = Entry.Decrypt(data, pkg, meta);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (var o = File.Create(target))
                    o.Write(data, 0, (int)meta.DataSize);
            }

            // Some update dumps lack the external param.sfo. Preserve the PKG's own metadata.
            if (pkg.ParamSfo?.ParamSfo != null)
            {
                Directory.CreateDirectory(Path.Combine(destination, "sce_sys"));
                File.WriteAllBytes(Path.Combine(destination, "sce_sys", "param.sfo"), pkg.ParamSfo.ParamSfo.Serialize());
            }
        }

        static string LocateGame(string folder)
        {
            var matches = Files(folder).Where(p => Path.GetFileName(p).Equals("eboot.bin", StringComparison.OrdinalIgnoreCase)).Select(Path.GetDirectoryName).Distinct().ToArray();
            if (matches.Length != 1)
                throw new InvalidDataException(L.T("Arquivo sem um único jogo extraído (eboot.bin), ou sem PKG compatível."));
            return matches[0];
        }

        public static void Merge(string source, string destination, CancellationToken token)
        {
            foreach (string file in Files(source).ToArray())
            {
                token.ThrowIfCancellationRequested();
                string relative = file.Substring(Path.GetFullPath(source).TrimEnd('\\').Length + 1), target = SafeTarget(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (File.Exists(target))
                    File.Delete(target);
                File.Move(file, target);
            }
        }

        void PrepareInput(string[] sources, string destination, string temp, string password, bool update, bool bufferLast = false, string bufferMarker = null)
        {
            progress(0, update ? L.T("Preparando atualização...") : L.T("Preparando jogo base..."));
            if (sources.Length == 1)
            {
                using (var rar = StoredRar5.TryOpen(sources[0]))
                {
                    if (rar != null)
                    {
                        log(L.T("RAR5 armazenado detectado: verificando integridade antes da leitura direta..."));
                        Run7z("t", sources[0], null, password, false);
                        if (bufferLast)
                        {
                            log(L.T("Copiando o último volume pequeno para a RAM..."));
                            string buffered = rar.BufferLastSmallVolume();
                            if (buffered != null)
                            {
                                log("VOLUME_BUFFERED:" + buffered);
                                if (!string.IsNullOrWhiteSpace(bufferMarker))
                                {
                                    log(L.T("Aguardando confirmação da etapa de espaço temporário..."));
                                    while (!File.Exists(bufferMarker))
                                    {
                                        if (cancel.WaitHandle.WaitOne(250))
                                            cancel.ThrowIfCancellationRequested();
                                    }
                                }
                            }
                        }

                        log(L.T("Extraindo o PKG diretamente dos volumes RAR, sem cópia intermediária..."));
                        ExtractPackage(rar, destination, update);
                        return;
                    }
                }
            }

            string unpacked = UnpackInputs(sources, temp, password);
            string pkg = File.Exists(unpacked) ? unpacked : ChoosePackage(unpacked, update);
            if (pkg != null)
                ExtractPackage(pkg, destination, update);
            else
            {
                string game = LocateGame(unpacked);
                var sfoPath = Path.Combine(game, "sce_sys", "param.sfo");
                if (!File.Exists(sfoPath))
                    throw new InvalidDataException(L.T("Pasta extraída sem param.sfo. Selecione o PKG original."));
                ParamSfo sfo;
                using (var s = File.OpenRead(sfoPath))
                    sfo = ParamSfo.FromStream(s);
                if (sfo["TITLE_ID"]?.ToString().Trim('\0', ' ') != "CUSA00223" || (update && sfo["APP_VER"]?.ToString().Trim('\0', ' ') != "01.07"))
                    throw new InvalidDataException(L.T("Pasta extraída incompatível com CUSA00223 / update 1.07."));
                Merge(game, destination, cancel);
            }
        }

        static void Cleanup(string stage, string parent)
        {
            if (!Path.GetFullPath(stage).StartsWith(Path.GetFullPath(parent).TrimEnd('\\') + "\\.iss-preparador-", StringComparison.OrdinalIgnoreCase))
                throw new IOException(L.T("Pasta temporária fora do local esperado."));
            // Never follow reparse points while deleting an application's own temporary directory.
            foreach (string p in Directory.GetFileSystemEntries(stage))
            {
                var attrs = File.GetAttributes(p);
                if ((attrs & FileAttributes.Directory) != 0 && (attrs & FileAttributes.ReparsePoint) == 0)
                    CleanupChild(p);
                else if ((attrs & FileAttributes.Directory) != 0)
                    Directory.Delete(p);
                else
                    File.Delete(p);
            }

            Directory.Delete(stage);
        }

        static void CleanupChild(string folder)
        {
            foreach (var p in Directory.GetFileSystemEntries(folder))
            {
                var a = File.GetAttributes(p);
                if ((a & FileAttributes.Directory) != 0)
                {
                    if ((a & FileAttributes.ReparsePoint) == 0)
                        CleanupChild(p);
                    else
                        Directory.Delete(p);
                }
                else
                    File.Delete(p);
            }

            Directory.Delete(folder);
        }

        public string Run(Job job)
        {
            string destination = Path.GetFullPath(job.Destination), parent = Path.GetDirectoryName(destination);
            if (Directory.Exists(destination) || File.Exists(destination))
                throw new IOException(L.T("Escolha uma pasta final NOVA. Uma instalação existente não será sobrescrita."));
            if (!Directory.Exists(parent))
                throw new DirectoryNotFoundException(L.T("A pasta que receberá a instalação não existe."));
            string[] baseSources = ResolveSources(job.Base, job.BaseFiles), updateSources = string.IsNullOrWhiteSpace(job.UpdateReadyMarker) ? ResolveSources(job.Update, job.UpdateFiles) : null;
            if (updateSources != null && baseSources.Intersect(updateSources, StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidDataException(L.T("Selecione arquivos diferentes para jogo e update."));
            log(L.T("Seleção: ") + (job.BaseFiles?.Length ?? 1) + L.T(" arquivo(s) da base; ") + (job.UpdateFiles?.Length ?? 1) + L.T(" arquivo(s) do update. Volumes do mesmo conjunto são lidos pela primeira parte."));
            string stage = Path.Combine(parent, ".iss-preparador-" + Guid.NewGuid().ToString("N")), game = Path.Combine(stage, "game"), patch = Path.Combine(stage, "update");
            Directory.CreateDirectory(stage);
            try
            {
                log(L.T("Preparando jogo base..."));
                PrepareInput(baseSources, game, Path.Combine(stage, "archive-base"), job.BasePassword, false, job.BufferLastRarVolume, job.BufferedVolumeReleaseMarker);
                string baseArchive = Path.Combine(stage, "archive-base");
                if (Directory.Exists(baseArchive))
                    CleanupChild(baseArchive);
                log("BASE_STAGE_COMPLETE");
                if (updateSources == null)
                {
                    log(L.T("Aguardando confirmação de download do update..."));
                    while (!File.Exists(job.UpdateReadyMarker))
                    {
                        if (cancel.WaitHandle.WaitOne(1000))
                            cancel.ThrowIfCancellationRequested();
                    }

                    updateSources = ResolveSources(job.Update, job.UpdateFiles);
                    if (baseSources.Intersect(updateSources, StringComparer.OrdinalIgnoreCase).Any())
                        throw new InvalidDataException(L.T("Base e update não podem ser o mesmo arquivo."));
                }

                log(L.T("Preparando atualização 1.07..."));
                PrepareInput(updateSources, patch, Path.Combine(stage, "archive-update"), job.UpdatePassword, true);
                progress(0, L.T("Unindo base e update..."));
                log(L.T("Unindo arquivos: o update substitui os arquivos correspondentes da base..."));
                Merge(patch, game, cancel);
                cancel.ThrowIfCancellationRequested();
                foreach (string relative in new[]
                {
                    "eboot.bin",
                    "sce_module/libc.prx",
                    "sce_module/libSceFios2.prx",
                    "sce_sys/param.sfo",
                    "playgo.pgm",
                    "sce_discmap.plt",
                    "sce_discmap_patch.plt"
                }

                )
                    if (!File.Exists(SafeTarget(game, relative)))
                        throw new InvalidDataException(L.T("Instalação incompleta. Falta: ") + relative);
                foreach (string relative in new[]
                {
                    "art/cache",
                    "art/movies",
                    "art/ui"
                }

                )
                    if (!Directory.Exists(SafeTarget(game, relative)))
                        throw new InvalidDataException(L.T("Pasta de conteúdo ausente: ") + relative);
                if (new FileInfo(Path.Combine(game, "eboot.bin")).Length == 0)
                    throw new InvalidDataException(L.T("eboot.bin vazio."));
                var all = Files(game).ToArray();
                log(L.T("Verificação estrutural concluída: ") + all.Length + L.T(" arquivos. Não equivale a uma verificação por hashes de referência."));
                File.WriteAllText(Path.Combine(game, "PREPARACAO.txt"), L.T("CUSA00223 + update 1.07\r\nBase extraída antes do update. Verificação estrutural concluída.\r\nPreparador local, sem arquivos de jogos incluídos.\r\n"), Encoding.UTF8);
                cancel.ThrowIfCancellationRequested();
                Directory.Move(game, destination);
                progress(100, L.T("Concluído"));
                log(L.T("Pronto: ") + destination);
                return destination;
            }
            finally
            {
                try
                {
                    Cleanup(stage, parent);
                }
                catch (Exception ex)
                {
                    log(L.T("Não foi possível limpar temporários: ") + stage + " — " + ex.Message);
                }
            }
        }
    }
}