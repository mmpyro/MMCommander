using System.Collections.Generic;
using System.Threading.Tasks;

namespace IOLib
{
    public interface IFileSystemManager
    {
        List<IAbstractFileStructure> GetAllStructures(string path);
        List<IAbstractFileStructure> GetFilesFromPath(string path);
        List<IAbstractFileStructure> GetDirectoriesFromPath(string path);
        Task<List<IAbstractFileStructure>> GetFilesFromPathAsync(string path);
        Task<List<IAbstractFileStructure>> GetDirectoriesFromPathAsync(string path);
        Task<List<IAbstractFileStructure>> GetAllStructuresAsync(string path);
        void CreateDirectory(string path);
        void CreateFile(string path);
        void FileExist(string path);
        void Split(IAbstractFileStructure abstractFile, int partSize, FileSizeUnit unit, string destinationPath);
        void Join(string sourcePath, string fileName, string destinationFileName);
        IAbstractFileStructure GetDirFromPath(string path);
        IEnumerable<DriveStruct> GetDrives();
    }

    public class FileSystemManager : IFileSystemManager
    {
        private readonly IFileManager _fileManager;
        private readonly IDriveManager _driveManager;
        private readonly IDirectoryManager _directoryManager;
        private readonly IComparer<IAbstractFileStructure> _comparer;

        public FileSystemManager(IFileManager fileManager, IDriveManager driveManager, IDirectoryManager directoryManager, IComparer<IAbstractFileStructure> comparer)
        {
            _fileManager = fileManager;
            _driveManager = driveManager;
            _directoryManager = directoryManager;
            _comparer = comparer;
        }

        public List<IAbstractFileStructure> GetAllStructures(string path)
        {
            var dirs = _directoryManager.GetDirectoriesFromPath(path);
            dirs.Sort(_comparer);
            var files = _directoryManager.GetFilesFromPath(path);
            files.Sort(_comparer);
            dirs.AddRange(files);
            return dirs;
        }

        public Task<List<IAbstractFileStructure>> GetAllStructuresAsync(string path)
        {
            return Task.Run(() =>
            {
                return GetAllStructures(path);
            });
        }

        public List<IAbstractFileStructure> GetFilesFromPath(string path)
        {
            return _directoryManager.GetFilesFromPath(path);
        }

        public List<IAbstractFileStructure> GetDirectoriesFromPath(string path)
        {
            return _directoryManager.GetDirectoriesFromPath(path);
        }

        public Task<List<IAbstractFileStructure>> GetFilesFromPathAsync(string path)
        {
            return _directoryManager.GetFilesFromPathAsync(path);
        }

        public Task<List<IAbstractFileStructure>> GetDirectoriesFromPathAsync(string path)
        {
            return _directoryManager.GetDirectoriesFromPathAsync(path);
        }

        public void CreateDirectory(string path)
        {
            _directoryManager.CreateDirectory(path);
        }

        public void CreateFile(string path)
        {
            _fileManager.CreateFile(path);
        }

        public void FileExist(string path)
        {
            _fileManager.FileExist(path);
        }

        public void Split(IAbstractFileStructure abstractFile, int partSize, FileSizeUnit unit, string destinationPath)
        {
            _fileManager.Split(abstractFile, partSize, unit, destinationPath);
        }

        public void Join(string sourcePath, string fileName, string destinationFileName)
        {
            _fileManager.Join(sourcePath, fileName, destinationFileName);
        }

        public IAbstractFileStructure GetDirFromPath(string path)
        {
            return _directoryManager.GetDirFromPath(path);
        }

        public IEnumerable<DriveStruct> GetDrives()
        {
            return _driveManager.GetDrives();
        }
    }
}