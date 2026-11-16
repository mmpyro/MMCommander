# Commander redesign: design source files

These files are exports from the "Commander Redesign — Avalonia Dark" design canvas.
Each `*.dc.html` file is one screen. The files need a canvas runtime (`support.js`) that is not in the repo,
so they do not render in a browser on their own.

Read them as an exact spec:
- The inline `style="…"` attributes hold every color, size, spacing, radius and font.
- The `<script type="text/x-dc">` block at the bottom of each file holds the sample data and the state logic.

Implement Direction A. B and C are alternatives that stay here for reference.

| File | Screen |
|---|---|
| Main.dc.html | A · Graphite Classic main window (THE target design) |
| Tokens.dc.html | Color tokens, type, row states, Avalonia mapping |
| CopyFlow.dc.html | F5 copy: options → progress → conflict |
| DeleteDialog.dc.html | F8 delete confirmation (trash / permanent) |
| CompareDirs.dc.html | Compare directories |
| Settings.dc.html | Settings › Appearance |
| Shortcuts.dc.html | Settings › Keyboard |
| Sidebar.dc.html | B · alternative (sidebar + command palette), reference only |
| Inspector.dc.html | C · alternative (inspector + transfer queue), reference only |
