using System.Collections.Generic;
using IOLib;

namespace Comander.Dtos
{
    // Input for the F5/F6 dialog.
    public class CopyDialogModel
    {
        public bool IsMove { get; init; }
        public IReadOnlyList<string> Names { get; init; }
        public long TotalSize { get; init; }
        public string Destination { get; init; }
        public string FreeSpaceText { get; init; }
        public ConflictPolicy Policy { get; init; } = ConflictPolicy.Ask;
        // The panel has a filter, so "Only matching filter" makes sense.
        public bool HasFilter { get; init; }
    }

    public class CopyRequest
    {
        public string Destination { get; init; }
        public TransferOptions Options { get; init; } = new TransferOptions();
        public bool OnlyMatchingFilter { get; init; }
        // false = "Add to queue": run after the current job without a progress window.
        public bool ShowProgress { get; init; } = true;
    }

    public class DeleteItem
    {
        public string Name { get; init; }
        public string SizeText { get; init; }
    }

    // Input for the F8 dialog.
    public class DeleteDialogModel
    {
        public IReadOnlyList<DeleteItem> Items { get; init; }
        public string Folder { get; init; }
        public string TotalSizeText { get; init; }
        public bool Permanent { get; init; }
        // Why Trash can't be used for these items (another volume, ...); forces permanent mode.
        public string TrashUnavailable { get; init; }
    }

    public class DeleteRequest
    {
        public bool Permanent { get; init; }
        // "Don't ask again when moving to Trash".
        public bool DontAskAgain { get; init; }
    }
}
