using System.Collections.Generic;
using System.Linq;
using Comander.CommanderIO;

namespace Comander.Core
{
    public class FileMarker
    {
        private readonly IEnumerable<IMetadataFileStructure> _firstFileSet;
        private readonly IEnumerable<IMetadataFileStructure> _secondFileSet;
        private readonly FileParametersComparer _fileParametersComparer;

        public FileMarker(IEnumerable<IMetadataFileStructure> firstFileSet, IEnumerable<IMetadataFileStructure> secondFileSet,
            FileParametersComparer fileParametersComparer)
        {
            _firstFileSet = firstFileSet;
            _secondFileSet = secondFileSet;
            _fileParametersComparer = fileParametersComparer;
        }

        public IEnumerable<IMetadataFileStructure> Mark()
        {
            foreach (var file in _firstFileSet)
            {
                if (file.IsDirectory == false)
                {
                    var secondFile = _secondFileSet.FirstOrDefault(t => t.Name.Equals(file.Name));
                    if (!_fileParametersComparer.Compare(file, secondFile))
                        file.SetColor("Red");
                }
            }
            return _firstFileSet;
        }

    }

    public class FileParametersComparer
    {
        public bool Compare(IMetadataFileStructure file, IMetadataFileStructure secondFile)
        {
            if (secondFile == null)
                return false;
            else
            {
                return file.Size.Equals(secondFile.Size)
                       && file.LastModifyTime.Equals(secondFile.LastModifyTime)
                       && file.IsReadOnly.Equals(secondFile.IsReadOnly);
            }
        }
    }
}