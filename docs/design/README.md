# Commander redesign — design source files

Exported from the "Commander Redesign — Avalonia Dark" design canvas.
Each `*.dc.html` file is one screen. They use a canvas runtime (`support.js`)
that is not included, so they will not render in a browser on their own.
Read them as an exact spec: every color, size, spacing, radius and font is in the
inline `style="…"` attributes, and sample data / state logic is in the
`<script type="text/x-dc">` block at the bottom of each file.

Implement Direction A. B and C are alternatives kept for reference.

- Main.dc.html — A · Graphite Classic main window (THE target design)
- Tokens.dc.html — color tokens, type, row states, Avalonia mapping
- CopyFlow.dc.html — F5 copy: options → progress → conflict
- DeleteDialog.dc.html — F8 delete confirmation (trash / permanent)
- CompareDirs.dc.html — compare directories
- Settings.dc.html — Settings › Appearance
- Shortcuts.dc.html — Settings › Keyboard
- Sidebar.dc.html — B · alternative (sidebar + command palette), reference only
- Inspector.dc.html — C · alternative (inspector + transfer queue), reference only
