using System;
using System.Collections.Generic;
using System.IO;

namespace Comander.Core
{
    public interface IPathResolver
    {
        bool ContainsPath(string value);
        string GetPath(string value);
    }

    public class PathResolver : IPathResolver
    {
        private readonly IDictionary<string, string> _dict;

        public PathResolver()
        {
            _dict = new Dictionary<string, string>()
            {
                {"%temp%", Path.GetTempPath()},
                {"%programdata%", GetFolderPath(Environment.SpecialFolder.ApplicationData)},
                {"%windows%", GetFolderPath(Environment.SpecialFolder.Windows)},
                {"%desktop%", GetFolderPath(Environment.SpecialFolder.Desktop)},
                {"%pf%", GetFolderPath(Environment.SpecialFolder.ProgramFiles)},
                {"%pf86%", GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)},
                {"%documents%", GetFolderPath(Environment.SpecialFolder.MyDocuments)},
            };
        }

        private string GetFolderPath(Environment.SpecialFolder specialFolder)
        {
            return Environment.GetFolderPath(specialFolder);
        }

        public bool ContainsPath(string value)
        {
            return _dict.ContainsKey(value.ToLower());
        }

        public string GetPath(string value)
        {
            string outputValue;
            return _dict.TryGetValue(value.ToLower(), out outputValue) ? outputValue : string.Empty;
        }
    }
}