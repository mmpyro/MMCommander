# Install

Download the archive for your system from the
[latest release](https://github.com/mmpyro/MMCommander/releases/latest):

| System | File |
|---|---|
| Windows (x64) | `MMCommander-<version>-win-x64.zip` |
| Linux (x64) | `MMCommander-<version>-linux-x64.tar.gz` |
| macOS (Apple silicon) | `MMCommander-<version>-osx-arm64.tar.gz` |

Each build is self-contained, so you don't need to install .NET.

## Check the download

Download `SHA256SUMS.txt` from the same release, then:

=== "Linux"

    ```sh
    sha256sum -c SHA256SUMS.txt --ignore-missing
    ```

=== "macOS"

    ```sh
    shasum -a 256 -c SHA256SUMS.txt --ignore-missing
    ```

=== "Windows (PowerShell)"

    ```powershell
    Get-FileHash MMCommander-*-win-x64.zip -Algorithm SHA256
    ```

    Compare the hash with the `win-x64` line in `SHA256SUMS.txt`.

## Unpack and run

Unpack the archive and run `Commander` (`Commander.exe` on Windows). Keep all the files in one folder. The app reads
`Config.xml`, `Commands.xml` and `Manuals/` from that folder.

### macOS

The build is not signed or notarized, so Gatekeeper blocks the first start. Run this once on the unpacked folder:

```sh
xattr -dr com.apple.quarantine MMCommander-<version>-osx-arm64
```

### Windows

The build is not signed, so SmartScreen may show "Windows protected your PC" on the first start. Click
**More info**, then **Run anyway**.

### Linux

The archive keeps the executable bit. If your desktop does not start it, run `./Commander` from a terminal.

## Updates

Once a day, when it starts, the app looks for a newer release on GitHub. It can also install that release for you.
See [Settings → Updates](features/settings.md#updates).

## Build it yourself

You need the .NET SDK 10.

```sh
dotnet run --project Commander
dotnet publish Commander -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

Use `win-x64` or `osx-arm64` for the other systems. Keep `Config.xml`, `Commands.xml`, `Manuals/` and `Plugins/`
next to the binary.
