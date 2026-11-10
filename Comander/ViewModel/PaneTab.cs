using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Comander.ViewModel
{
    // One folder tab in a panel: its current path plus its own back/forward history.
    public class PaneTab : ObservableObject
    {
        private string _path;
        private bool _isActive;

        public PaneTab(string path)
        {
            _path = path;
        }

        public Stack<string> Back { get; } = new Stack<string>();
        public Stack<string> Forward { get; } = new Stack<string>();

        public string Path
        {
            get => _path;
            set
            {
                if (SetProperty(ref _path, value))
                    OnPropertyChanged(nameof(Title));
            }
        }

        // Folder name; a drive root ("/", "C:\") shows as itself.
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_path)) return string.Empty;
                var name = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(_path));
                return string.IsNullOrEmpty(name) ? _path : name;
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }

    // One clickable part of the breadcrumb.
    public class PathSegment
    {
        public PathSegment(string name, string fullPath, bool isLast, bool isRoot)
        {
            Name = name;
            FullPath = fullPath;
            IsLast = isLast;
            ShowSeparator = !isLast && !isRoot;
        }

        public string Name { get; }
        public string FullPath { get; }
        public bool IsLast { get; }
        // "/ Users › me": the root already ends in a separator.
        public bool ShowSeparator { get; }

        public static List<PathSegment> Split(string path)
        {
            var result = new List<PathSegment>();
            if (string.IsNullOrEmpty(path)) return result;
            var parts = new List<string>();
            for (var dir = new DirectoryInfo(path); dir != null; dir = dir.Parent)
                parts.Insert(0, dir.FullName);
            for (int i = 0; i < parts.Count; i++)
            {
                var name = i == 0 ? parts[i] : System.IO.Path.GetFileName(parts[i]);
                result.Add(new PathSegment(name, parts[i], i == parts.Count - 1, i == 0));
            }
            return result;
        }
    }
}
