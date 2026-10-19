#!/usr/bin/env bash
# Converts an SVG logo to a PNG that meets NuGet's package icon requirements:
# PNG or JPEG, at most 1 MB, and square (128x128 recommended).
# See https://learn.microsoft.com/nuget/reference/msbuild-targets#packageicon
#
# Usage: scripts/convert-logo.sh [input.svg] [output.png] [size]
#   input.svg   SVG to convert (default: logo.svg at the repository root)
#   output.png  PNG to write (default: input path with a .png extension)
#   size        Width and height in pixels (default: 128)
#
# Uses the first available of rsvg-convert, inkscape or ImageMagick.

set -euo pipefail

readonly MAX_BYTES=$((1024 * 1024))

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
input="${1:-${repo_root}/logo.svg}"
output="${2:-${input%.svg}.png}"
size="${3:-128}"

if [[ ! -f "${input}" ]]; then
  echo "Input SVG not found: ${input}" >&2
  exit 1
fi
if ! [[ "${size}" =~ ^[1-9][0-9]*$ ]]; then
  echo "Size must be a positive integer: ${size}" >&2
  exit 1
fi

if command -v rsvg-convert > /dev/null; then
  rsvg-convert --width "${size}" --height "${size}" --keep-aspect-ratio --format png --output "${output}" "${input}"
elif command -v inkscape > /dev/null; then
  inkscape "${input}" --export-type=png --export-filename="${output}" \
    --export-width="${size}" --export-height="${size}" 2> /dev/null
elif command -v magick > /dev/null; then
  magick -background none -density 384 "${input}" -resize "${size}x${size}" \
    -gravity center -extent "${size}x${size}" "PNG32:${output}"
else
  echo "No SVG converter found. Install librsvg (rsvg-convert), Inkscape or ImageMagick." >&2
  exit 1
fi

bytes="$(wc -c < "${output}")"
if (( bytes > MAX_BYTES )); then
  echo "Converted icon is ${bytes} bytes, over NuGet's 1 MB limit: ${output}" >&2
  exit 1
fi

echo "Wrote ${output} (${size}x${size}, ${bytes} bytes)"
