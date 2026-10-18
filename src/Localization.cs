// GPL-3.0-or-later
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Preparador
{
    static class L
    {
        public static string Language { get; private set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt-BR" : "en";
        internal static string PreferenceFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Preparador-inFAMOUS", "language.txt");
        public static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"PREPARADOR", "EXTRACTOR"},
            {"inFAMOUS Second Son — Preparador", "inFAMOUS Second Son — Extractor"},
            {"SUA INSTALAÇÃO", "YOUR INSTALLATION"},
            {"JOGO BASE", "BASE GAME"},
            {"ATUALIZAÇÃO", "UPDATE"},
            {"PASTA FINAL", "OUTPUT FOLDER"},
            {"Todas as partes juntas", "All parts together"},
            {"Base + update unidos", "Base + update merged"},
            {"ARQUIVOS PRESERVADOS", "ORIGINAL FILES PRESERVED"},
            {"Seus pacotes originais\npermanecem intactos.", "Your original archives\nremain untouched."},
            {"PRONTO PARA PREPARAR", "READY TO PREPARE"},
            {"Selecione uma ou todas as partes do jogo.", "Select one or all game archive parts."},
            {"Selecione o update e suas partes, se houver.", "Select the update and its parts, if split."},
            {"PREPARE O SEU JOGO", "PREPARE YOUR GAME"},
            {"PRONTO PARA SEATTLE.", "READY FOR SEATTLE."},
            {"01  JOGO BASE", "01  BASE GAME"},
            {"02  UPDATE 1.07", "02  UPDATE 1.07"},
            {"03  PASTA FINAL", "03  OUTPUT FOLDER"},
            {"ANDAMENTO DA PREPARAÇÃO", "PREPARATION PROGRESS"},
            {"Escolha os arquivos da base, o update e uma pasta nova para começar.", "Choose the base files, the update and a new output folder to begin."},
            {"EXTRAIR E UNIR", "EXTRACT & MERGE"},
            {"Cancelar", "Cancel"},
            {"Abrir pasta final", "Open output folder"},
            {"CANCELANDO E LIMPANDO TEMPORÁRIOS...", "CANCELLING AND CLEANING TEMPORARY FILES..."},
            {"Caminho informado manualmente; demais volumes serão encontrados na pasta.", "Path entered manually; other volumes are located in the same folder."},
            {"CANCELANDO. AGUARDE A LIMPEZA ANTES DE FECHAR.", "CANCELLING. WAIT FOR CLEANUP BEFORE CLOSING."},
            {"PASTA NOVA", "NEW FOLDER"},
            {"Escolher pasta", "Choose folder"},
            {"Selecionar partes", "Select parts"},
            {"O update será aplicado por cima da base, em uma pasta nova.", "The update is applied over the base game in a new folder."},
            {"Senha", "Password"},
            {"Escolha onde criar a pasta final", "Choose where to create the output folder"},
            {"Selecione uma ou todas as partes — ", "Select one or all parts — "},
            {"Arquivos de jogo|*.pkg;*.rar;*.zip;*.001;*.002;*.003;*.r00;*.z01|Todos os arquivos|*.*", "Game files|*.pkg;*.rar;*.zip;*.001;*.002;*.003;*.r00;*.z01|All files|*.*"},
            {" arquivo(s) selecionado(s) · ", " selected file(s) · "},
            {"demais volumes lidos automaticamente", "other volumes read automatically"},
            {"ZIPs extraídos em sequência", "ZIPs extracted in sequence"},
            {"Seleção de partes", "Archive part selection"},
            {"PREPARANDO...", "PREPARING..."},
            {"CONCLUÍDO · SELECIONE A PASTA NO LAUNCHER DO JOGO", "DONE · SELECT THE FOLDER IN THE GAME LAUNCHER"},
            {"Arquivos originais preservados.", "Original files preserved."},
            {"OPERAÇÃO CANCELADA", "OPERATION CANCELLED"},
            {"Cancelado. Arquivos de entrada preservados.", "Cancelled. Input files preserved."},
            {"NÃO FOI POSSÍVEL CONCLUIR", "UNABLE TO COMPLETE"},
            {"ERRO: ", "ERROR: "},
            {"Preparação interrompida", "Preparation interrupted"},
            {"Caminho inválido no arquivo: ", "Invalid path in archive: "},
            {"Caminho inseguro no arquivo: ", "Unsafe path in archive: "},
            {"Caminho fora da pasta de trabalho.", "Path is outside the working folder."},
            {"Falta a primeira parte RAR (part1). Mantenha todas as partes na mesma pasta.", "The first RAR part (part1) is missing. Keep all parts in the same folder."},
            {"Parte não encontrada: ", "Part not found: "},
            {"Selecione partes do mesmo arquivo RAR/ZIP dividido. ZIPs independentes podem ser selecionados juntos; não misture PKGs ou conjuntos RAR diferentes.", "Select parts of the same split RAR/ZIP archive. Independent ZIPs can be selected together; do not mix PKGs or different RAR sets."},
            {"Falta a primeira parte: ", "First part missing: "},
            {"Espaço insuficiente. Esta etapa precisa de pelo menos ", "Insufficient space. This stage requires at least "},
            {" GB livres na unidade ", " GB free on drive "},
            {"Falta tools/7z.exe. Extraia o ZIP inteiro do aplicativo.", "Missing tools/7z.exe. Extract the entire application ZIP."},
            {"Não foi possível abrir/extrair o arquivo. Confira senha, partes ausentes ou download corrompido. 7-Zip: ", "Unable to open/extract the archive. Check the password, missing parts or a corrupted download. 7-Zip: "},
            {"Links simbólicos dentro de arquivos não são aceitos.", "Symbolic links inside archives are not accepted."},
            {"Links dentro de arquivos não são aceitos.", "Links inside archives are not accepted."},
            {"Link encontrado na pasta extraída.", "Link found in the extracted folder."},
            {"Verificando volumes de ", "Checking archive volumes: "},
            {"Descompactando ", "Extracting "},
            {" (aguarde)...", " (please wait)..."},
            {"Pacote adicional ignorado: ", "Additional package skipped: "},
            {"Esperado exatamente um PKG ", "Expected exactly one "},
            {"de update", "update PKG"},
            {"base", "base"},
            {" CUSA00223. Encontrados: ", " CUSA00223 package. Found: "},
            {". Selecione o arquivo correto.", ". Select the correct file."},
            {"PKG incompatível: ", "Incompatible PKG: "},
            {". Use CUSA00223 e o tipo correto.", ". Use CUSA00223 and the correct package type."},
            {"O update precisa ser 1.07. Detectado: ", "Update 1.07 is required. Detected: "},
            {"PKG confirmado: ", "PKG confirmed: "},
            {" versão ", " version "},
            {". Extraindo PFS corrigido...", ". Extracting PFS with the corrected reader..."},
            {"Chave PFS indisponível. Este PKG não é compatível com a biblioteca.", "PFS key unavailable. This PKG is not compatible with the library."},
            {"pfs_image.dat ausente.", "Missing pfs_image.dat."},
            {"Nomes duplicados no PKG.", "Duplicate file names in PKG."},
            {"Tamanho extraído incorreto.", "Incorrect extracted file size."},
            {"Jogo", "Game"},
            {" arquivos", " files"},
            {"Metadado excessivamente grande.", "Metadata is too large."},
            {"Arquivo sem um único jogo extraído (eboot.bin), ou sem PKG compatível.", "Archive does not contain exactly one extracted game (eboot.bin) or a compatible PKG."},
            {"Preparando atualização...", "Preparing update..."},
            {"Preparando jogo base...", "Preparing base game..."},
            {"RAR5 armazenado detectado: verificando integridade antes da leitura direta...", "Stored RAR5 detected: checking integrity before direct reading..."},
            {"Copiando o último volume pequeno para a RAM...", "Buffering the small final volume in RAM..."},
            {"Aguardando confirmação da etapa de espaço temporário...", "Waiting for temporary-space stage confirmation..."},
            {"Extraindo o PKG diretamente dos volumes RAR, sem cópia intermediária...", "Extracting PKG directly from RAR volumes without an intermediate copy..."},
            {"Pasta extraída sem param.sfo. Selecione o PKG original.", "Extracted folder has no param.sfo. Select the original PKG."},
            {"Pasta extraída incompatível com CUSA00223 / update 1.07.", "Extracted folder is incompatible with CUSA00223 / update 1.07."},
            {"Pasta temporária fora do local esperado.", "Temporary folder is outside the expected location."},
            {"Escolha uma pasta final NOVA. Uma instalação existente não será sobrescrita.", "Choose a NEW output folder. An existing installation will not be overwritten."},
            {"A pasta que receberá a instalação não existe.", "The parent folder for the installation does not exist."},
            {"Selecione arquivos diferentes para jogo e update.", "Select different files for the base game and the update."},
            {"Seleção: ", "Selection: "},
            {" arquivo(s) da base; ", " base file(s); "},
            {" arquivo(s) do update. Volumes do mesmo conjunto são lidos pela primeira parte.", " update file(s). Volumes in the same set are read starting with the first part."},
            {"Aguardando confirmação de download do update...", "Waiting for update download confirmation..."},
            {"Base e update não podem ser o mesmo arquivo.", "Base and update cannot be the same file."},
            {"Preparando atualização 1.07...", "Preparing update 1.07..."},
            {"Unindo base e update...", "Merging base game and update..."},
            {"Unindo arquivos: o update substitui os arquivos correspondentes da base...", "Merging files: update files replace the corresponding base files..."},
            {"Instalação incompleta. Falta: ", "Incomplete installation. Missing: "},
            {"Pasta de conteúdo ausente: ", "Missing content folder: "},
            {"eboot.bin vazio.", "eboot.bin is empty."},
            {"Verificação estrutural concluída: ", "Structural check complete: "},
            {" arquivos. Não equivale a uma verificação por hashes de referência.", " files. This is not a comparison against reference hashes."},
            {"CUSA00223 + update 1.07\r\nBase extraída antes do update. Verificação estrutural concluída.\r\nPreparador local, sem arquivos de jogos incluídos.\r\n", "CUSA00223 + update 1.07\r\nBase extracted before update. Structural check completed.\r\nLocal preparation tool. No game files included.\r\n"},
            {"Concluído", "Done"},
            {"Pronto: ", "Ready: "},
            {"Não foi possível limpar temporários: ", "Unable to clean temporary files: "},
            {"RAR5: inteiro inválido.", "RAR5: invalid integer."},
            {"RAR: falta um volume intermediário.", "RAR: an intermediate volume is missing."},
            {"Cabeçalho RAR5 excessivo.", "RAR5 header is too large."},
            {"CRC de cabeçalho RAR5 incorreto.", "Incorrect RAR5 header CRC."},
            {"Nome RAR5 excessivo.", "RAR5 file name is too long."},
            {"Volumes RAR pertencem a arquivos diferentes.", "RAR volumes belong to different files."},
            {"Conjunto RAR incompleto ou sequência inválida.", "Incomplete RAR set or invalid volume sequence."},
            {"RAR5: extras inválidos.", "RAR5: invalid extra records."},
            {"RAR5: extra truncado.", "RAR5: truncated extra record."},
            {"Volume RAR incompleto.", "Incomplete RAR volume."},
            {"RAR5 truncado.", "Truncated RAR5."},
            {"Seek negativo.", "Negative seek offset."}
        };

        public static string T(string text)
        {
            string translated;
            return Language == "en" && English.TryGetValue(text, out translated) ? translated : text;
        }

        public static void SetLanguage(string language, bool save = false)
        {
            if (language != "pt-BR" && language != "en") throw new ArgumentException("Unsupported language");
            Language = language;
            if (save)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(PreferenceFile)); File.WriteAllText(PreferenceFile, language); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public static void LoadPreference()
        {
            try { string saved = File.ReadAllText(PreferenceFile).Trim(); if (saved == "en" || saved == "pt-BR") SetLanguage(saved); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        // Translate only application labels. Never translate paths, passwords or existing log output.
        public static void Apply(Control control)
        {
            if (!(control is TextBox) && !(control is ComboBox))
            {
                string source = control.Text;
                foreach (var pair in English) if (pair.Value == source) { source = pair.Key; break; }
                control.Text = T(source);
            }
            foreach (Control child in control.Controls) Apply(child);
            control.Invalidate();
        }
    }
}
