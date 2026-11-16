using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IOLib
{
    public delegate void FindFileDelegate(IAbstractFileStructure file);

    public interface IDirectoryManager
    {
        List<IAbstractFileStructure> GetFilesFromPath(string path);
        List<IAbstractFileStructure> GetDirectoriesFromPath(string path);
        void SearchFiles(SearchParameters searchParameters);
        void CreateDirectory(string path);
        IAbstractFileStructure GetDirFromPath(string actualPath);
        event FindFileDelegate OnFindFile;
    }

    public class DirectoryManager : IDirectoryManager
    {
        private readonly IFileFactory _fileFactory;
        public event FindFileDelegate OnFindFile;


        public DirectoryManager(IFileFactory fileFactory)
        {
            _fileFactory = fileFactory;
        }

        // Errors propagate so the panel can say why a folder can't be listed (BUG-13).
        public List<IAbstractFileStructure> GetFilesFromPath(string path)
        {
            return Directory.GetFiles(path).Select(file => _fileFactory.CreateFileMsg(new FileInfo(file))).ToList();
        }

        public List<IAbstractFileStructure> GetDirectoriesFromPath(string path)
        {
            var list = new List<IAbstractFileStructure>();
            CreateParentDirIfNotNull(list, path);
            list.AddRange(Directory.GetDirectories(path).Select(dir => _fileFactory.CreateFileMsg(new DirectoryInfo(dir))));
            return list;
        }

        public void SearchFiles(SearchParameters searchParameters)
        {
            string phrase = searchParameters.SearchPhrase;
            if (searchParameters.MatchOptions == MatchOptions.WholeWord)
                phrase = string.Format(@"\b{0}\b", phrase);
            else if (phrase.Contains('*'))
                // A '*' means a glob ("*.txt", "foo*"), not a regex.
                phrase = "^" + Regex.Escape(phrase).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var pattern = new Regex(phrase, searchParameters.RegexOptions);
            var pending = new Stack<string>();
            pending.Push(searchParameters.StartDirectoryPath);
            while (pending.Count > 0)
            {
                string dir = pending.Pop();
                string[] files, dirs;
                try
                {
                    files = Directory.GetFiles(dir);
                    dirs = searchParameters.Recursive ? Directory.GetDirectories(dir) : Array.Empty<string>();
                }
                catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
                {
                    continue; // one unreadable folder must not end the search (BUG-13)
                }
                // Match the file name, not the full path (BUG-12).
                foreach (var file in files.Where(f => pattern.IsMatch(Path.GetFileName(f))))
                    OnFindedFileNotification(_fileFactory.CreateFileMsg(file));
                // Skip symlinks/junctions: they can loop.
                foreach (var sub in dirs.Reverse().Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.ReparsePoint)))
                    pending.Push(sub);
            }
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IAbstractFileStructure GetDirFromPath(string path)
        {
            return _fileFactory.GetDirectoryFromPath(path);
        }

        private void OnFindedFileNotification(IAbstractFileStructure file)
        {
            if (OnFindFile != null)
                OnFindFile(file);
        }

        private void CreateParentDirIfNotNull(List<IAbstractFileStructure> list, string path)
        {
            var parent = Directory.GetParent(path);
            if (parent != null)
                list.Add(_fileFactory.CreateParentDirectoryMsg(parent));
        }
    }
}