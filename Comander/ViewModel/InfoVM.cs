using System;
using System.Collections.Generic;
using System.IO;
using Comander.CommanderIO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Comander.ViewModel
{
    public class InfoVM : ObservableObject
    {
        private static readonly (FileAttributes Flag, string Word)[] Words =
        {
            (FileAttributes.ReadOnly, "Read-only"), (FileAttributes.Hidden, "Hidden"), (FileAttributes.System, "System"),
            (FileAttributes.Archive, "Archive"), (FileAttributes.Compressed, "Compressed"), (FileAttributes.Encrypted, "Encrypted"),
            (FileAttributes.Temporary, "Temporary"), (FileAttributes.ReparsePoint, "Symbolic link"),
        };

        private IMetadataFileStructure _file;

        public InfoVM(IMetadataFileStructure file)
        {
            _file = file;
            AttributesText = Describe(file.Attributes);
            Permissions = UnixPermissions(file.FullName);
        }

        public IMetadataFileStructure File
        {
            get { return _file; }
            set { SetProperty(ref _file, value); }
        }

        public string AttributesText { get; }

        /// <summary>"rw-r--r--  (644)" on macOS/Linux; null on Windows or when the mode can't be read.</summary>
        public string Permissions { get; }

        public static string Describe(FileAttributes attributes)
        {
            var words = new List<string>();
            foreach (var (flag, word) in Words)
                if (attributes.HasFlag(flag))
                    words.Add(word);
            return words.Count == 0 ? "None" : string.Join(", ", words);
        }

        private static string UnixPermissions(string path)
        {
            if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(path))
                return null;
            try
            {
                var mode = (int)System.IO.File.GetUnixFileMode(path) & 0x1FF;
                var chars = new char[9];
                for (int i = 0; i < 9; i++)
                    chars[i] = (mode & (1 << (8 - i))) != 0 ? "rwx"[i % 3] : '-';
                return $"{new string(chars)}  ({Convert.ToString(mode, 8)})";
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
