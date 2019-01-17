using System;
using SevenZip;

namespace ZipLib
{
    public class ZipAdapter
    {
        public ZipAdapter()
        {
            if(Environment.Is64BitProcess)
                SevenZipCompressor.SetLibraryPath(String.Format(@"{0}\dll\7z.dll", AppDomain.CurrentDomain.BaseDirectory ));
            else
                SevenZipCompressor.SetLibraryPath(String.Format(@"{0}\dll\7z(x86).dll", AppDomain.CurrentDomain.BaseDirectory ));
        }

        public void CompressFiles(ZipParameters zipParameters ,params string[] filePaths)
        {
            var cmp = InitCompressor(zipParameters);
            cmp.CompressFiles(zipParameters.ArchiveName, filePaths);
        }

        /// <summary>
        /// Works as extract all
        /// </summary>
        /// <param name="zipParameters"></param>
        /// <param name="dir"></param>
        public void UnCompressFile(ZipParameters zipParameters, string dir)
        {
            SevenZipExtractor extractor = null;
            if(string.IsNullOrEmpty(zipParameters.Password))
                extractor = new SevenZipExtractor(zipParameters.ArchiveName);
            else
                extractor = new SevenZipExtractor(zipParameters.ArchiveName, zipParameters.Password);
            extractor.ExtractArchive(dir);
        }

        public void CompressFilesWithEncryption(ZipParameters zipParameters, params string[] filePaths)
        {
            var cmp = InitCompressor(zipParameters);
            cmp.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
            cmp.EncryptHeaders = true;
            cmp.CompressFilesEncrypted(zipParameters.ArchiveName, zipParameters.Password,filePaths);
        }

        public void CompressDir(ZipParameters zipParameters, string dir)
        {
            var cmp = InitCompressor(zipParameters);
            cmp.CompressDirectory(dir, zipParameters.ArchiveName);
        }

        public void CompressDirWithEncryption(ZipParameters zipParameters, string dir)
        {
            var cmp = InitCompressor(zipParameters);
            cmp.CompressDirectory(dir, zipParameters.ArchiveName, zipParameters.Password);
        }

        private SevenZipCompressor InitCompressor(ZipParameters zipParameters)
        {
            var cmp = new SevenZipCompressor
            {
                CompressionMethod = CompressionMethod.Lzma,
                CompressionMode = CompressionMode.Create,
                CompressionLevel = CompressionLevel.Fast,
                VolumeSize = 0
            };
            switch (zipParameters.Type)
            {
                case ZipType.Zip:
                    cmp.ArchiveFormat = OutArchiveFormat.Zip;
                    zipParameters.ArchiveName = zipParameters.ArchiveName + "." + "zip";
                    break;
                case ZipType.GZip:
                    cmp.ArchiveFormat = OutArchiveFormat.GZip;
                    zipParameters.ArchiveName = zipParameters.ArchiveName + "." + "gzip";
                    break;
                case ZipType.SevenZip:
                    cmp.ArchiveFormat = OutArchiveFormat.SevenZip;
                    zipParameters.ArchiveName = zipParameters.ArchiveName + "." + "7z";
                    break;
                case ZipType.Tar:
                    cmp.ArchiveFormat = OutArchiveFormat.Tar;
                    zipParameters.ArchiveName = zipParameters.ArchiveName + "." + "tar";
                    break;
            }
            return cmp;
        }
    }
}