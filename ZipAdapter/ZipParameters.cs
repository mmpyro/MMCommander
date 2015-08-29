namespace ZipLib
{
    public class ZipParameters
    {
        public string ArchiveName { get; set; }
        public string Password { get; set; }
        public ZipType Type { get; set; }

        public ZipParameters(string archiveName, string password, ZipType type = ZipType.Zip)
        {
            ArchiveName = archiveName;
            Password = password;
            Type = type;
        }
    }
}