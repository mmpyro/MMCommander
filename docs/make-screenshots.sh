#!/bin/sh
# Regenerates the images and clips in docs/images from a demo folder (CommanderTest/DocsScreenshots.cs).
# Needs ffmpeg (with libx264) for demo.gif and the MP4 clips. Run from the repo root: sh docs/make-screenshots.sh
set -e
out="$PWD/docs/images"
rm -rf "$out/frames"
MMC_DOCS="$out" dotnet test CommanderTest --filter FullyQualifiedName~DocsScreenshots
ffmpeg -loglevel error -y -framerate 1 -i "$out/frames/%03d.png" \
  -vf "format=rgb24,split[a][b];[a]palettegen=reserve_transparent=0[p];[b][p]paletteuse=dither=none" \
  -gifflags -transdiff "$out/demo.gif"
# One short loop per docs page (copy, filter, compare, palette); the last frame is held for 2 s.
for dir in "$out"/frames/*/; do
  name=$(basename "$dir")
  ffmpeg -loglevel error -y -framerate 2 -i "$dir%03d.png" \
    -vf "tpad=stop_mode=clone:stop_duration=2,scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p" \
    -c:v libx264 -crf 28 -movflags +faststart -an "$out/$name.mp4"
done
rm -rf "$out/frames"
ls -lh "$out"
