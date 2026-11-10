using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Comander.ViewModel
{
    public class PaletteItem
    {
        public PaletteItem(string label, string hint, string kind, Action run)
        {
            Label = label;
            Hint = hint;
            Kind = kind;
            Run = run;
        }

        public string Label { get; }
        // Key gesture for commands, "folder"/"file" for entries of the active pane.
        public string Hint { get; }
        public string Kind { get; }
        public Action Run { get; }
    }

    // Ctrl+K: fuzzy search over the command registry, the active pane's files, and typed folder paths.
    public class PaletteVM : ObservableObject
    {
        private const int MaxResults = 50;
        private readonly MainVM _vm;
        private bool _isOpen;
        private string _query = string.Empty;
        private List<PaletteItem> _results = new List<PaletteItem>();
        private PaletteItem _selected;

        public PaletteVM(MainVM vm)
        {
            _vm = vm;
            OpenCommand = new RelayCommand(Open);
            CloseCommand = new RelayCommand(Close);
            RunCommand = new RelayCommand(RunSelected);
        }

        public RelayCommand OpenCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand RunCommand { get; }

        public bool IsOpen
        {
            get => _isOpen;
            private set => SetProperty(ref _isOpen, value);
        }

        public string Query
        {
            get => _query;
            set
            {
                if (SetProperty(ref _query, value ?? string.Empty))
                    Update();
            }
        }

        public List<PaletteItem> Results
        {
            get => _results;
            private set => SetProperty(ref _results, value);
        }

        public PaletteItem Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public void Open()
        {
            _query = string.Empty;
            OnPropertyChanged(nameof(Query));
            Update();
            IsOpen = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            FocusActivePane(null);
        }

        public void Move(int delta)
        {
            if (Results.Count == 0) return;
            var index = Math.Clamp(Results.IndexOf(Selected) + delta, 0, Results.Count - 1);
            Selected = Results[index];
        }

        private void RunSelected()
        {
            var item = Selected;
            Close();
            item?.Run();
        }

        private void Update()
        {
            var items = new List<(int score, PaletteItem item)>();
            var q = _query.Trim();
            if (q.Length > 1 && Path.IsPathFullyQualified(q) && Directory.Exists(q))
                items.Add((1000, new PaletteItem("Go to " + q, "folder", "path", () => _vm.ActivePanel.ActualPath = q)));
            foreach (var command in CommandRegistry.All)
            {
                if (command.Id is CommandRegistry.MarkId or CommandRegistry.EditPathId or "palette") continue;
                var icommand = command.Resolve(_vm, _vm.ActivePanel);
                if (icommand == null || !icommand.CanExecute(null)) continue;
                var score = Score(command.Label, q);
                if (score > 0)
                    // Match quality first; on a tie a command beats a file.
                    items.Add((score * 10 + 1, new PaletteItem(command.Label, Hint(command), "command", () => icommand.Execute(null))));
            }
            if (q.Length > 0 && _vm.ActivePanel?.Files != null)
            {
                foreach (var file in _vm.ActivePanel.Files.Where(f => f.Name != ".."))
                {
                    var score = Score(file.Name, q);
                    if (score > 0)
                        items.Add((score * 10, new PaletteItem(file.Name, file.IsDirectory ? "folder" : "file", "file", () => JumpTo(file))));
                }
            }
            Results = items.OrderByDescending(i => i.score).Select(i => i.item).Take(MaxResults).ToList();
            Selected = Results.FirstOrDefault();
        }

        private string Hint(AppCommand command)
        {
            var gesture = _vm.KeyMap[command.Id];
            return gesture == null ? command.Group : Converters.GestureTextConverter.Format(gesture);
        }

        private void JumpTo(IMetadataFileStructure file)
        {
            _vm.ActivePanel.SelectedFile = file;
            FocusActivePane(file);
        }

        private void FocusActivePane(IMetadataFileStructure file)
        {
            if (_vm.ActivePanel != null)
                WeakReferenceMessenger.Default.Send(new SetFocusMessage { GridName = _vm.ActivePanel.Name, SelectedFile = file });
        }

        // 3 = starts with, 2 = contains, 1 = letters in order (fuzzy), 0 = no match. An empty query matches every command.
        public static int Score(string text, string query)
        {
            if (string.IsNullOrEmpty(query)) return 1;
            if (string.IsNullOrEmpty(text)) return 0;
            var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index == 0) return 3;
            if (index > 0) return 2;
            int t = 0;
            foreach (var c in query)
            {
                t = text.IndexOf(char.ToString(c), t, StringComparison.OrdinalIgnoreCase);
                if (t < 0) return 0;
                t++;
            }
            return 1;
        }
    }
}
