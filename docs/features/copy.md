# Copy, move and delete

<video autoplay loop muted playsinline poster="../images/copy.png" aria-label="F5 opens the copy options, then the copy runs with a progress window">
  <source src="../images/copy.mp4" type="video/mp4">
</video>

**F5** (copy) and **F6** (move) open a dialog with these options:

- The destination.
- What to do when a file exists: ask, overwrite older, skip, or keep both.
- Keep the timestamps or not.
- Verify each copy with SHA-256.
- Follow symlinks or not.

Then the copy runs as a job in a progress window. In that window you can pause the job, cancel it or send it to the
background. **Add to queue** starts the job after the current job ends. If you mark nothing, the app uses the file
under the cursor.

**Shift+F5** copies in the same panel and keeps both files. The copy gets a new name.

## Conflicts

With "ask", the job stops at each file that exists. It shows the existing file and the incoming file side by side.
Choose **Replace**, **Skip** or **Keep both**. To use the same answer for the rest of the job, tick "Do this for all
remaining conflicts". You set the default policy in [Settings](settings.md).

## Delete

**F8** moves the files to the Trash: the Windows Recycle Bin, macOS `~/.Trash` or the Linux freedesktop Trash.
**Shift+F8** deletes them permanently. Both ask first.

## Clipboard

**Ctrl+C** copies the marked files to the system clipboard. **Ctrl+V** pastes files into the active panel. You can
use them to copy files between MMCommander and the file manager of your system.
