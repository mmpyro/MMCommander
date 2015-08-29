

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Comander.Annotations;

namespace Comander.ViewModel
{
    public class Folder
    {
        public string Name
        {
            get
            {
                if (!String.IsNullOrEmpty(Path))
                {
                    return System.IO.Path.GetFileName(Path);
                }
                return null;
            }
        }
        public string Path
        { get; set; }
        public List<Folder> Folders
        { get; set; }
        public Folder()
        {
            Folders = new List<Folder>();
        }
        public static Folder CreateFolderTree(string rootFolder)
        {
            Folder fld = new Folder { Path = rootFolder };
            foreach (var item in Directory.GetDirectories(rootFolder))
            {
                try
                {
                    fld.Folders.Add(CreateFolderTree(item));
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
        private List<Folder> _folders;

        public TreeManager()
        {
            Folders = new List<Folder>();
            Folders.Add( Folder.CreateFolderTree(@"E:\"));
        }

        public List<Folder> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                OnPropertyChanged("Folders");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
