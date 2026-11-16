using System.Collections.Generic;
using System.Threading.Tasks;

namespace IOLib
{
    public interface IFileSystemManager
    {
        List<IAbstractFileStructure> GetAllStructures(string path);
        Task<List<IAbstractFileStructure>> GetAllStructuresAsync(string path);
        void CreateDirectory(string path);
        void CreateFile(string path);
        IAbstractFileStructure GetDirFromPath(string path);
        IEnumerable<DriveStruct> GetDrives();
        DriveSpace GetSpace(string path);
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

        public void CreateDirectory(string path)
        {
            _directoryManager.CreateDirectory(path);
        }

        public void CreateFile(string path)
        {
            _fileManager.CreateFile(path);
        }

        public IAbstractFileStructure GetDirFromPath(string path)
        {
            return _directoryManager.GetDirFromPath(path);
        }

        public IEnumerable<DriveStruct> GetDrives()
        {
            return _driveManager.GetDrives();
        }

        public DriveSpace GetSpace(string path)
        {
            return _driveManager.GetSpace(path);
        }
    }
}