using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Comander.Annotations;
using System.Windows.Input;
using Comander.ViewModel.Commands;

namespace Comander.ViewModel
{
    public class File
    {
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(Path))
                {
                    return GetFileName(Path);
                }
                return null;
            }
        }
        public string Path{ get; set; }
        public bool  IsDir { get; set; }
        public List<File> Files { get; set; }

        public File()
        {
            Files = new List<File>();
        }

        private static string GetFileName(string fullName)
        {
            return System.IO.Path.GetFileName(fullName);
        }

        public static File CreateFolderTree(string rootFolder)
        {
            File fld = new File { Path = rootFolder, IsDir = true };
            foreach (var file in Directory.GetFiles(rootFolder))
            {
                var fi = new FileInfo(file);
                fld.Files.Add(new File
                {
                    Path = fi.FullName,
                    IsDir = false
                });
            }

            foreach (var dir in Directory.GetDirectories(rootFolder))
            {
                try
                {
                    fld.Files.Add(CreateFolderTree(dir));
                }
                catch
                {
                }
            }
            return fld;
        }
    }

    public class TreeManager : INotifyPropertyChanged
    {
        private List<File> _files;
        private readonly IOManager _manager;

        public TreeManager(string path, IOManager manager)
        {
            Folders = new List<File>();
            Folders.Add(File.CreateFolderTree(path));
            _manager = manager;
        }

        public void SetItemChange(File file)
        {
            if (file.IsDir)
                _manager.ActualPath = file.Path;
            else
            {
                string dirPath = file.Path.Substring(0, file.Path.Length - file.Name.Length);
                _manager.ActualPath = dirPath;
            }
        }

        public List<File> Folders
        {
            get { return _files; }
            set
            {
                _files = value;
                OnPropertyChanged("Folders");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
