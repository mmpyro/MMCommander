using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IOLib
{
    // A text file as lines, loaded so that saving an untouched file writes the same bytes back.
    public class TextDoc
    {
        // shortcut: bigger files are compared as binary (no line diff). Upgrade to streaming if needed.
        public const long MaxTextSize = 20 * 1024 * 1024;

        public string Path { get; private set; }
        public Encoding Encoding { get; private set; }
        public DateTime Modified { get; private set; }
        // Ending used for new lines: the one most lines of the file already have.
        public string NewLine { get; private set; }
        public List<string> Lines { get; private set; } = new List<string>();
        // Each line's own ending ("\r\n", "\n", "\r" or "" on an unterminated last line).
        public List<string> Ends { get; private set; } = new List<string>();

        /// <returns>Null when the file is not text: too big, has NUL bytes, or would not save back byte for byte.</returns>
        public static TextDoc Load(string path)
        {
            if (new FileInfo(path).Length > MaxTextSize) return null;
            var modified = File.GetLastWriteTimeUtc(path);
            var bytes = File.ReadAllBytes(path);

            Encoding encoding;
            if (StartsWith(bytes, 0xEF, 0xBB, 0xBF)) encoding = new UTF8Encoding(true, true);
            // UTF-32 LE starts like the UTF-16 LE BOM, so it is checked first.
            else if (StartsWith(bytes, 0xFF, 0xFE, 0, 0)) encoding = new UTF32Encoding(false, true, true);
            else if (StartsWith(bytes, 0, 0, 0xFE, 0xFF)) encoding = new UTF32Encoding(true, true, true);
            else if (StartsWith(bytes, 0xFF, 0xFE)) encoding = new UnicodeEncoding(false, true, true);
            else if (StartsWith(bytes, 0xFE, 0xFF)) encoding = new UnicodeEncoding(true, true, true);
            else if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8192)) >= 0) return null;
            else encoding = new UTF8Encoding(false, true);

            int bom = encoding.GetPreamble().Length;
            string text;
            try
            {
                text = encoding.GetString(bytes, bom, bytes.Length - bom);
            }
            catch (DecoderFallbackException)
            {
                if (bom > 0) return null;
                // Not UTF-8: Latin-1 maps every byte to one char, so nothing is lost on save.
                encoding = Encoding.Latin1;
                text = encoding.GetString(bytes);
            }

            var doc = new TextDoc { Path = path, Encoding = encoding, Modified = modified };
            doc.Split(text);
            return doc.ToBytes().AsSpan().SequenceEqual(bytes) ? doc : null;
        }

        private static bool StartsWith(byte[] bytes, params byte[] prefix) => bytes.Length >= prefix.Length && bytes.AsSpan(0, prefix.Length).SequenceEqual(prefix);

        private void Split(string text)
        {
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\r' && text[i] != '\n') continue;
                int length = text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
                Lines.Add(text.Substring(start, i - start));
                Ends.Add(text.Substring(i, length));
                i += length - 1;
                start = i + 1;
            }
            if (start < text.Length)
            {
                Lines.Add(text.Substring(start));
                Ends.Add(string.Empty);
            }
            NewLine = Ends.Where(e => e.Length > 0).GroupBy(e => e).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? Environment.NewLine;
        }

        public void Insert(int index, string line)
        {
            Lines.Insert(index, line);
            Ends.Insert(index, NewLine);
        }

        public void RemoveAt(int index)
        {
            Lines.RemoveAt(index);
            Ends.RemoveAt(index);
        }

        // Replaces `remove` lines at `at` with `lines`. A file without a final newline keeps having none.
        public void Replace(int at, int remove, IList<string> lines)
        {
            bool unterminated = Ends.Count > 0 && Ends[Ends.Count - 1].Length == 0;
            for (int i = 0; i < remove; i++) RemoveAt(at);
            for (int i = 0; i < lines.Count; i++) Insert(at + i, lines[i]);
            if (unterminated && Ends.Count > 0)
                Ends[Ends.Count - 1] = string.Empty;
        }

        public byte[] ToBytes()
        {
            var text = new StringBuilder();
            for (int i = 0; i < Lines.Count; i++)
            {
                text.Append(Lines[i]);
                // Only the last line may be unterminated.
                text.Append(Ends[i].Length == 0 && i < Lines.Count - 1 ? NewLine : Ends[i]);
            }
            return Encoding.GetPreamble().Concat(Encoding.GetBytes(text.ToString())).ToArray();
        }

        public bool ChangedOnDisk => !File.Exists(Path) || File.GetLastWriteTimeUtc(Path) != Modified;

        // Writes a temp file next to the target and renames it over, so a crash never leaves half a file.
        public void Save()
        {
            // A symlink keeps pointing at its file: the target is replaced, not the link.
            var target = File.Exists(Path) ? File.ResolveLinkTarget(Path, true)?.FullName ?? Path : Path;
            // The rename below only needs a writable folder; a read-only file must not be replaced.
            if (File.Exists(target))
                using (new FileStream(target, FileMode.Open, FileAccess.Write)) { }
            var temp = target + ".mmc-tmp";
            try
            {
                File.WriteAllBytes(temp, ToBytes());
                if (!OperatingSystem.IsWindows() && File.Exists(target))
                    File.SetUnixFileMode(temp, File.GetUnixFileMode(target));
                File.Move(temp, target, true);
            }
            finally
            {
                File.Delete(temp);
            }
            Modified = File.GetLastWriteTimeUtc(Path);
        }

        public (List<string> Lines, List<string> Ends) Snapshot() => (new List<string>(Lines), new List<string>(Ends));

        public void Restore((List<string> Lines, List<string> Ends) snapshot)
        {
            Lines = new List<string>(snapshot.Lines);
            Ends = new List<string>(snapshot.Ends);
        }
    }
}
