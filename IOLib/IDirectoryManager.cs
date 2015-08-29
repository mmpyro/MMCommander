﻿using System;
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
        void SearchFiles(SearchParameters searchParameters, MatchOptions matchOption = MatchOptions.WholeWord);
        void SearchFiles(SearchParameters searchParameters, RegexOptions comparsion);
        Task<List<IAbstractFileStructure>> GetFilesFromPathAsync(string path);
        Task<List<IAbstractFileStructure>> GetDirectoriesFromPathAsync(string path);
        Task SearchFilesAsync(SearchParameters searchParameters, MatchOptions matchOption = MatchOptions.WholeWord);
        Task SearchFilesAsync(SearchParameters searchParameters, RegexOptions comparsion);
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

        public List<IAbstractFileStructure> GetFilesFromPath(string path)
        {
            try
            {
                var res = Directory.GetFiles(path).Select(AbstractFileStructure => _fileFactory.CreateFileMsg(new FileInfo(AbstractFileStructure)));    
                return  new List<IAbstractFileStructure>(res);
            }
            catch
            {
                return _fileFactory.CreateEmptyFileList();
            }
        }

        public List<IAbstractFileStructure> GetDirectoriesFromPath(string path)
        {
            try
            {
                var res = Directory.GetDirectories(path).Select(AbstractFileStructure => _fileFactory.CreateFileMsg(new DirectoryInfo(AbstractFileStructure)));
                return new List<IAbstractFileStructure>(res);
            }
            catch
            {
                return _fileFactory.CreateEmptyFileList();
            }
        }

        public void SearchFiles(SearchParameters searchParameters, MatchOptions matchOption = MatchOptions.WholeWord)
        {
            string name = searchParameters.SearchFrase.ToLower();
            string fullName = searchParameters.StartDirectory.FullName;
            var files = GetFilesFromPath(fullName);
            foreach (var file in files)
            {
                if (matchOption == MatchOptions.WholeWord && file.Name.ToLower().Equals(name))
                    OnFindedFileNotification(file);
                else if (file.Name.ToLower().Contains(name))
                    OnFindedFileNotification(file);
            }

            if(searchParameters.Recursive)
                GetDirectoriesFromPath(fullName).ForEach(t =>
                    SearchFiles(new SearchParameters(t, searchParameters.SearchFrase, searchParameters.Recursive), matchOption));
        }

        public void SearchFiles(SearchParameters searchParameters, RegexOptions comparasion)
        {
            string fullName = searchParameters.StartDirectory.FullName;
            var files = GetFilesFromPath(fullName);
            foreach (var file in files)
            {
                if(Regex.IsMatch(file.Name, searchParameters.SearchFrase, comparasion))
                    OnFindedFileNotification(file);
            }
            if(searchParameters.Recursive)
                GetDirectoriesFromPath(fullName)
                    .ForEach(t => SearchFiles(new SearchParameters(t, searchParameters.SearchFrase, searchParameters.Recursive), comparasion));
        }

        public Task<List<IAbstractFileStructure>> GetFilesFromPathAsync(string path)
        {
            return Task<List<IAbstractFileStructure>>.Run(() => GetFilesFromPath(path));
        }

        public Task<List<IAbstractFileStructure>> GetDirectoriesFromPathAsync(string path)
        {
            return Task<List<IAbstractFileStructure>>.Run(() => GetDirectoriesFromPath(path));
        }

        public Task SearchFilesAsync(SearchParameters searchParameters, MatchOptions matchOption = MatchOptions.WholeWord)
        {
            return Task.Run(() => SearchFiles(searchParameters, matchOption));
        }

        public Task SearchFilesAsync(SearchParameters searchParameters, RegexOptions comparsion)
        {
            return Task.Run(() => SearchFiles(searchParameters, comparsion));
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
    }
}