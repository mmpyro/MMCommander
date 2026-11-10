namespace ZipLib
{
    // Archive formats we can write. Extraction detects the format from content (zip, 7z, rar, tar, gz, bz2).
    public enum ZipType
    {
        Zip,
        Tar,
        GZip,
        Bzip2
    }
}
