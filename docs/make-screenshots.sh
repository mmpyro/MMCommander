#!/bin/sh
# Regenerates the README images in docs/images from a demo folder (ComanderTest/DocsScreenshots.cs).
# Needs ffmpeg for demo.gif. Run from the repo root: sh docs/make-screenshots.sh
set -e
out="$PWD/docs/images"
rm -rf "$out/frames"
MMC_DOCS="$out" dotnet test ComanderTest --filter FullyQualifiedName~DocsScreenshots
ffmpeg -loglevel error -y -framerate 1 -i "$out/frames/%03d.png" \
  -vf "format=rgb24,split[a][b];[a]palettegen=reserve_transparent=0[p];[b][p]paletteuse=dither=none" \
  -gifflags -transdiff "$out/demo.gif"
rm -rf "$out/frames"
ls -lh "$out"
