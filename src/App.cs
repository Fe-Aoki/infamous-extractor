// GPL-3.0-or-later
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.IO.Compression;
using LibOrbisPkg.SFO;
using System.Xml;
using System.Xml.Serialization;

namespace Preparador
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 3 && args[0] == "--prepare")
            {
                using (var log = new StreamWriter(args[2], false, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                }

                )
                {
                    try
                    {
                        Job job;
                        using (var input = XmlReader.Create(args[1], new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                            job = (Job)new XmlSerializer(typeof(Job)).Deserialize(input);
                        var engine = new Engine(s => log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + s), (n, s) => log.WriteLine("PROGRESS " + n + " " + s), CancellationToken.None);
                        string result = engine.Run(job);
                        log.WriteLine("PREPARE_COMPLETE:" + result);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine("PREPARE_FAILED:" + ex);
                        return 1;
                    }
                }
            }

            if (args.Length == 2 && args[0] == "--selftest")
            {
                try
                {
                    SelfTest.Run(args[1]);
                    return 0;
                }
                catch (Exception ex)
                {
                    Directory.CreateDirectory(args[1]);
                    File.WriteAllText(Path.Combine(args[1], "FAILED.txt"), ex.ToString());
                    return 1;
                }
            }

            L.LoadPreference();
            Application.Run(new MainForm());
            return 0;
        }
    }

    static class SelfTest
    {
        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        static byte[] V(ulong number)
        {
            var result = new System.Collections.Generic.List<byte>();
            do
            {
                byte b = (byte)(number & 127);
                number >>= 7;
                if (number != 0)
                    b |= 128;
                result.Add(b);
            }
            while (number != 0);
            return result.ToArray();
        }

        static void Header(Stream output, Action<BinaryWriter> write)
        {
            using (var body = new MemoryStream())
            {
                using (var writer = new BinaryWriter(body, System.Text.Encoding.UTF8, true))
                    write(writer);
                byte[] size = V((ulong)body.Length), record = size.Concat(body.ToArray()).ToArray();
                using (var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(StoredRar5.Crc(record));
                    writer.Write(record);
                }
            }
        }

        static string[] StoredVolumes(string pkg, string folder)
        {
            byte[] data = File.ReadAllBytes(pkg);
            var paths = new string[3];
            for (int i = 0; i < 3; i++)
            {
                int begin = data.Length * i / 3, end = data.Length * (i + 1) / 3;
                byte[] part = data.Skip(begin).Take(end - begin).ToArray();
                int index = i;
                paths[i] = Path.Combine(folder, "stored.part" + (i + 1) + ".rar");
                using (var output = File.Create(paths[i]))
                {
                    output.Write(new byte[] { 82, 97, 114, 33, 26, 7, 1, 0 }, 0, 8);
                    Header(output, w =>
                    {
                        w.Write(V(1));
                        w.Write(V(0));
                        w.Write(V(3));
                        w.Write(V((ulong)index));
                    });
                    Header(output, w =>
                    {
                        w.Write(V(2));
                        w.Write(V((ulong)(2 | (index > 0 ? 8 : 0) | (index < 2 ? 16 : 0))));
                        w.Write(V((ulong)part.Length));
                        w.Write(V(4));
                        w.Write(V((ulong)data.Length));
                        w.Write(V(0));
                        w.Write(StoredRar5.Crc(index < 2 ? part : data));
                        w.Write(V(0));
                        w.Write(V(0));
                        byte[] name = System.Text.Encoding.UTF8.GetBytes("sample.pkg");
                        w.Write(V((ulong)name.Length));
                        w.Write(name);
                    });
                    output.Write(part, 0, part.Length);
                    Header(output, w =>
                    {
                        w.Write(V(5));
                        w.Write(V(0));
                        w.Write(V((ulong)(index < 2 ? 1 : 0)));
                    });
                }
            }

            return paths;
        }

        static LibOrbisPkg.PFS.FSDir Tree(string directory, LibOrbisPkg.PFS.FSDir parent = null)
        {
            var node = new LibOrbisPkg.PFS.FSDir
            {
                name = Path.GetFileName(directory),
                Parent = parent
            };
            foreach (var dir in Directory.GetDirectories(directory))
                node.Dirs.Add(Tree(dir, node));
            foreach (var file in Directory.GetFiles(directory))
                node.Files.Add(new LibOrbisPkg.PFS.FSFile(file) { name = Path.GetFileName(file), Parent = node });
            return node;
        }

        static void BuildPackage(string directory, string output, bool update)
        {
            new LibOrbisPkg.PKG.PkgBuilder(new LibOrbisPkg.PKG.PkgProperties { RootDir = Tree(directory), VolumeType = update ? LibOrbisPkg.GP4.VolumeType.pkg_ps4_patch : LibOrbisPkg.GP4.VolumeType.pkg_ps4_app, ContentId = "UP0001-CUSA00223_00-TESTPACKAGE00001", Passcode = new string ('0', 32), TimeStamp = new DateTime(2026, 10, 18) }).Write(output, s =>
            {
            });
        }

        static void SevenZip(string arguments)
        {
            using (var p = Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "7z.exe"), arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            {
                var stdout = p.StandardOutput.ReadToEndAsync();
                var stderr = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(30000))
                {
                    p.Kill();
                    throw new Exception("7z fixture timeout");
                }

                Check(p.ExitCode == 0, "7z fixture failed: " + stderr.Result + stdout.Result);
            }
        }

        static void SfoFile(string dir, bool update)
        {
            Directory.CreateDirectory(Path.Combine(dir, "sce_sys"));
            var sfo = new ParamSfo();
            sfo.SetValue("TITLE_ID", SfoEntryType.Utf8, "CUSA00223", 16);
            sfo.SetValue("APP_VER", SfoEntryType.Utf8, update ? "01.07" : "01.00", 8);
            sfo.SetValue("CATEGORY", SfoEntryType.Utf8, update ? "gp" : "gd", 4);
            File.WriteAllBytes(Path.Combine(dir, "sce_sys", "param.sfo"), sfo.Serialize());
        }

        public static void Run(string output)
        {
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            L.PreferenceFile = Path.Combine(output, "test-language.txt");
            L.SetLanguage("pt-BR");
            string fixture = Path.Combine(output, "fixtures-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fixture);
            var results = new System.Collections.Generic.List<string>();
            foreach (string bad in new[]
            {
                "../escape",
                "a/../../escape",
                "C:\\escape",
                "safe/CON.txt",
                "safe/a.",
                "/absolute"
            }

            )
            {
                bool rejected = false;
                try
                {
                    Engine.SafeTarget(fixture, bad);
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Check(rejected, "Unsafe path accepted: " + bad);
            }

            results.Add("PASS: bloqueio de caminhos inseguros");
            File.WriteAllText(Path.Combine(fixture, "game.part01.rar"), "");
            File.WriteAllText(Path.Combine(fixture, "game.part03.rar"), "");
            Check(Engine.FirstVolume(Path.Combine(fixture, "game.part03.rar")).EndsWith("part01.rar"), "First RAR volume");
            results.Add("PASS: identificação de primeira parte RAR");
            File.WriteAllText(Path.Combine(fixture, "game.part02.rar"), "");
            var rarParts = new[]
            {
                Path.Combine(fixture, "game.part03.rar"),
                Path.Combine(fixture, "game.part01.rar"),
                Path.Combine(fixture, "game.part02.rar")
            };
            Check(Engine.ResolveSources(rarParts[0], rarParts).Length == 1, "Selected RAR volumes not grouped");
            results.Add("PASS: seleção simultânea das 3 partes RAR agrupada pela primeira parte");
            string b = Path.Combine(fixture, "base"), u = Path.Combine(fixture, "update");
            Directory.CreateDirectory(b);
            Directory.CreateDirectory(u);
            SfoFile(b, false);
            SfoFile(u, true);
            foreach (var name in new[]
            {
                "eboot.bin",
                "sce_module/libc.prx",
                "sce_module/libSceFios2.prx",
                "playgo.pgm",
                "sce_discmap.plt",
                "sce_discmap_patch.plt",
                "art/cache/a",
                "art/movies/b",
                "art/ui/c"
            }

            )
            {
                string path = Path.Combine(b, name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "base");
            }

            File.WriteAllText(Path.Combine(u, "eboot.bin"), "update");
            File.WriteAllText(Path.Combine(u, "new.txt"), "new");
            string bz = Path.Combine(fixture, "base com espaço.zip"), uz = Path.Combine(fixture, "update.zip");
            ZipFile.CreateFromDirectory(b, bz);
            ZipFile.CreateFromDirectory(u, uz);
            string dest = Path.Combine(fixture, "result");
            var engine = new Engine(s => results.Add(s), (n, s) =>
            {
            }, CancellationToken.None);
            engine.Run(new Job { Base = bz, Update = uz, Destination = dest });
            Check(File.ReadAllText(Path.Combine(dest, "eboot.bin")) == "update", "Update did not replace base");
            Check(File.Exists(Path.Combine(dest, "new.txt")), "Update new file");
            Check(File.Exists(bz) && File.Exists(uz), "Input modified");
            Check(!Directory.GetDirectories(fixture, ".iss-preparador-*").Any(), "Temporary folders remain");
            results.Add("PASS: ZIP → base + update → união → validação → limpeza; originais preservados");
            bool existingRejected = false;
            try
            {
                engine.Run(new Job { Base = bz, Update = uz, Destination = dest });
            }
            catch (IOException)
            {
                existingRejected = true;
            }

            Check(existingRejected, "Existing destination overwritten");
            results.Add("PASS: destino existente protegido");
            string extra1 = Path.Combine(fixture, "extra1"), extra2 = Path.Combine(fixture, "extra2");
            Directory.CreateDirectory(extra1);
            Directory.CreateDirectory(extra2);
            File.WriteAllText(Path.Combine(extra1, "parte2.txt"), "part2");
            File.WriteAllText(Path.Combine(extra2, "parte3.txt"), "part3");
            string z2 = Path.Combine(fixture, "base.part2.zip"), z3 = Path.Combine(fixture, "base.part3.zip");
            ZipFile.CreateFromDirectory(extra1, z2);
            ZipFile.CreateFromDirectory(extra2, z3);
            string multi = Path.Combine(fixture, "multi-zip-result");
            engine.Run(new Job { Base = bz, BaseFiles = new[] { bz, z2, z3 }, Update = uz, Destination = multi });
            Check(File.Exists(Path.Combine(multi, "parte2.txt")) && File.Exists(Path.Combine(multi, "parte3.txt")), "Three ZIP inputs not combined");
            results.Add("PASS: seleção de 3 ZIPs independentes extraídos e unidos com o update");
            var canceled = new CancellationTokenSource();
            canceled.Cancel();
            bool cancellationPassed = false;
            try
            {
                new Engine(s =>
                {
                }, (n, s) =>
                {
                }, canceled.Token).Run(new Job { Base = bz, Update = uz, Destination = Path.Combine(fixture, "cancelled") });
            }
            catch (OperationCanceledException)
            {
                cancellationPassed = true;
            }

            Check(cancellationPassed && !Directory.Exists(Path.Combine(fixture, "cancelled")), "Cancellation failed");
            results.Add("PASS: cancelamento sem publicar instalação parcial");
            string bpkg = Path.Combine(fixture, "base.pkg"), upkg = Path.Combine(fixture, "update.pkg");
            BuildPackage(b, bpkg, false);
            BuildPackage(u, upkg, true);
            string pdest = Path.Combine(fixture, "pkg-result");
            engine.Run(new Job { Base = bpkg, Update = upkg, Destination = pdest });
            Check(File.ReadAllText(Path.Combine(pdest, "eboot.bin")) == "update", "PKG merge failed");
            results.Add("PASS: PKGs sintéticos base/update → PFS → união e metadados");
            string[] stored = StoredVolumes(bpkg, fixture);
            string storedDest = Path.Combine(fixture, "stored-rar-result");
            engine.Run(new Job { Base = stored[0], BaseFiles = stored, Update = upkg, Destination = storedDest, BufferLastRarVolume = true });
            Check(File.ReadAllText(Path.Combine(storedDest, "eboot.bin")) == "update" && stored.All(File.Exists), "Direct stored RAR extraction failed");
            results.Add("PASS: PKG direto de 3 RAR5 armazenados, CRC e buffer em RAM, sem apagar volumes");
            string split = Path.Combine(fixture, "partes jogo.zip"), password = "senha com espaco e \"aspas\"\\";
            SevenZip("a -tzip -v1k " + Engine.Quote("-p" + password) + " " + Engine.Quote(split) + " " + Engine.Quote(Path.Combine(b, "*")));
            string first = split + ".001";
            Check(File.Exists(first) && File.Exists(split + ".002"), "Split fixture missing second volume");
            string selected = split + ".002";
            string sdest = Path.Combine(fixture, "split-result");
            engine.Run(new Job { Base = selected, Update = uz, BasePassword = password, Destination = sdest });
            Check(File.ReadAllText(Path.Combine(sdest, "eboot.bin")) == "update", "Split/password extraction failed");
            results.Add("PASS: ZIP dividido, seleção de volume e senha com espaço/aspas/barra");
            bool wrongPassword = false;
            try
            {
                engine.Run(new Job { Base = first, Update = uz, BasePassword = "errada", Destination = Path.Combine(fixture, "wrong-password") });
            }
            catch (InvalidDataException)
            {
                wrongPassword = true;
            }

            Check(wrongPassword && !Directory.Exists(Path.Combine(fixture, "wrong-password")), "Wrong password not handled");
            results.Add("PASS: senha incorreta sem instalação parcial");
            string evil = Path.Combine(fixture, "unsafe.zip");
            using (var archive = ZipFile.Open(evil, ZipArchiveMode.Create))
            {
                using (var writer = new StreamWriter(archive.CreateEntry("../outside.txt").Open()))
                    writer.Write("bad");
            }

            bool traversalBlocked = false;
            try
            {
                engine.Run(new Job { Base = evil, Update = uz, Destination = Path.Combine(fixture, "evil-result") });
            }
            catch (InvalidDataException)
            {
                traversalBlocked = true;
            }

            Check(traversalBlocked && !File.Exists(Path.Combine(fixture, "outside.txt")), "Archive traversal failed");
            results.Add("PASS: ZIP com tentativa de escapar da pasta bloqueado antes da extração");
            var unpack = typeof(Engine).GetMethod("UnpackInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            string rar = Path.Combine(output, "rar-test.rar");
            if (File.Exists(rar))
            {
                string rout = Path.Combine(fixture, "rar-out");
                unpack.Invoke(engine, new object[] { rar, rout, "" });
                Check(new FileInfo(Path.Combine(rout, "stest1.txt")).Length == 2048, "RAR extraction failed");
                results.Add("PASS: RAR5 real descompactado e tamanho verificado");
            }
            else
                results.Add("SKIP: RAR5 fixture opcional não fornecida");
            string linkrar = Path.Combine(output, "rar-link-test.rar");
            if (File.Exists(linkrar))
            {
                bool blocked = false;
                try
                {
                    unpack.Invoke(engine, new object[] { linkrar, Path.Combine(fixture, "rar-link-out"), "" });
                }
                catch (System.Reflection.TargetInvocationException e)
                {
                    blocked = e.InnerException is InvalidDataException;
                }

                Check(blocked, "RAR symlink not blocked");
                results.Add("PASS: RAR com link simbólico bloqueado antes da extração");
            }

            L.SetLanguage("en", true);
            L.SetLanguage("pt-BR");
            L.LoadPreference();
            Check(L.Language == "en", "Language preference was not restored");
            results.Add("PASS: preferência de idioma salva e restaurada em arquivo de teste isolado");
            var englishLog = new System.Collections.Generic.List<string>();
            new Engine(englishLog.Add, (n, text) => { }, CancellationToken.None).Run(new Job { Base = bz, Update = uz, Destination = Path.Combine(fixture, "english-result") });
            Check(englishLog.Any(s => s == "Preparing base game...") && englishLog.Any(s => s.StartsWith("Ready: ")), "Engine log was not translated");
            Check(File.ReadAllText(Path.Combine(fixture, "english-result", "PREPARACAO.txt")).Contains("Structural check completed."), "Preparation report was not translated");
            bool englishError = false;
            try { Engine.ResolveSources(Path.Combine(fixture, "missing.pkg"), null); }
            catch (FileNotFoundException ex) { englishError = ex.Message.StartsWith("Part not found: "); }
            Check(englishError, "Application error was not translated");
            results.Add("PASS: extração e união em inglês, log, relatório final e erro traduzidos");
            L.SetLanguage("pt-BR");
            using (var form = new MainForm())
            {
                ((TextBox)typeof(MainForm).GetField("destination", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(form)).Text = @"C:\Jogos\inFAMOUS\Game-CUSA00223";
                form.Show();
                Application.DoEvents();
                using (var image = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                    image.Save(Path.Combine(output, "Interface.png"));
                }

                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var baseBox = (TextBox)typeof(MainForm).GetField("baseFile", flags).GetValue(form);
                var passBox = (TextBox)typeof(MainForm).GetField("basePassword", flags).GetValue(form);
                var selector = (LanguagePicker)typeof(MainForm).GetField("language", flags).GetValue(form);
                var startButton = (Button)typeof(MainForm).GetField("start", flags).GetValue(form);
                baseBox.Text = @"C:\Archives\game.part1.rar";
                passBox.Text = "test-password";
                selector.SelectedIndex = 1;
                Application.DoEvents();
                Check(form.Text.EndsWith("Extractor") && startButton.Text == "EXTRACT & MERGE", "Language selector did not update UI");
                Check(baseBox.Text == @"C:\Archives\game.part1.rar" && passBox.Text == "test-password", "Language switch changed inputs");
                selector.SelectedIndex = 0;
                Application.DoEvents();
                Check(startButton.Text == "EXTRAIR E UNIR" && passBox.Text == "test-password", "Portuguese round trip failed");
                baseBox.Clear(); passBox.Clear(); selector.SelectedIndex = 1;
                Application.DoEvents();
                using (var image = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                    image.Save(Path.Combine(output, "Interface-English.png"));
                }
                results.Add("PASS: troca Português ↔ English sem perder caminhos ou senhas; interface nos dois idiomas");
                form.Close();
            }

            results.Add("PASS: interface renderizada");
            File.WriteAllLines(Path.Combine(output, "RESULTADO.txt"), results);
        }
    }
}
