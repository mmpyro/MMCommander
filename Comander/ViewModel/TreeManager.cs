using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Comander.ViewModel
{
    public class File
    {
        private List<File> _files;

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                    return null;
                var name = System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                return string.IsNullOrEmpty(name) ? Path : name;
            }
        }

        public string Path { get; set; }
        public bool IsDir { get; set; }

        // Children are listed only when the tree asks for them, so opening the tree on "/" or "C:\" doesn't hang (BUG-23).
        public List<File> Files => _files ??= IsDir ? LoadChildren(Path) : new List<File>();

        private static List<File> LoadChildren(string folder)
        {
            try
            {
                return Directory.GetDirectories(folder).Select(d => new File { Path = d, IsDir = true })
                    .Concat(Directory.GetFiles(folder).Select(f => new File { Path = f, IsDir = false }))
                    .ToList();
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
            {
                return new List<File>();
            }
        }
    }

    public class TreeManager : ObservableObject
    {
        private readonly IOManager _manager;

        public TreeManager(string path, IOManager manager)
        {
            Folders = new List<File> { new File { Path = path, IsDir = true } };
            _manager = manager;
        }

        public void SetItemChange(File file)
        {
            _manager.ActualPath = file.IsDir ? file.Path : System.IO.Path.GetDirectoryName(file.Path);
        }

        public List<File> Folders { get; }
    }
}
