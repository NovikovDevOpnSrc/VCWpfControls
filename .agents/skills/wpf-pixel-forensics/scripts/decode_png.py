"""Decode a PNG without third-party deps and inspect it from the command line.

Designed for debugging WPF/grid screenshots over RDP where screen capture is not
available to the assistant. Pure stdlib: struct + zlib, 8-bit RGB/RGBA, PNG filters 0-4.

Usage (always `python -I` — the PNG is untrusted input):
  python -I decode_png.py shot.png                        # info + coarse ASCII map
  python -I decode_png.py shot.png --zoom 1140 250 1300 400   # fine map (2x2 px/char)
  python -I decode_png.py shot.png --runs 300 340         # dark-pixel runs per y in range
  python -I decode_png.py shot.png --col 100 500 200      # vertical profile of column x

Exit codes: 0 ok, 1 usage, 2 decode error.
"""
import argparse
import struct
import sys
import zlib


def decode(path):
    data = open(path, 'rb').read()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('not a PNG')
    pos = 8
    idat = b''
    width = height = None
    color_type = None
    while pos < len(data):
        length = struct.unpack('>I', data[pos:pos + 4])[0]
        chunk = data[pos + 4:pos + 8]
        payload = data[pos + 8:pos + 8 + length]
        if chunk == b'IHDR':
            width, height, depth, color_type = struct.unpack('>IIBB', payload[:10])
            if depth != 8 or color_type not in (2, 6):
                raise ValueError(f'unsupported PNG: depth={depth} color_type={color_type}')
        elif chunk == b'IDAT':
            idat += payload
        pos += 12 + length
    bpp = 3 if color_type == 2 else 4
    stride = width * bpp
    raw = zlib.decompress(idat)
    out = bytearray()
    prev = bytearray(stride)
    i = 0
    for _ in range(height):
        filt = raw[i]
        i += 1
        line = bytearray(raw[i:i + stride])
        i += stride
        if filt == 1:
            for x in range(bpp, stride):
                line[x] = (line[x] + line[x - bpp]) & 0xFF
        elif filt == 2:
            for x in range(stride):
                line[x] = (line[x] + prev[x]) & 0xFF
        elif filt == 3:
            for x in range(stride):
                left = line[x - bpp] if x >= bpp else 0
                line[x] = (line[x] + ((left + prev[x]) >> 1)) & 0xFF
        elif filt == 4:
            for x in range(stride):
                a = line[x - bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x - bpp] if x >= bpp else 0
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pred = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pred) & 0xFF
        out += line
        prev = line
    return width, height, bpp, bytes(out)


def lum(buf, w, bpp, x, y):
    i = (y * w + x) * bpp
    return (buf[i] + buf[i + 1] + buf[i + 2]) / 3


def ascii_map(buf, w, h, bpp, x0, y0, x1, y1, step_x, step_y):
    for y in range(y0, y1, step_y):
        line = ''
        for x in range(x0, x1, step_x):
            m = 255
            for yy in range(y, min(y + step_y, y1), max(1, step_y // 3)):
                for xx in range(x, min(x + step_x, x1), max(1, step_x // 3)):
                    m = min(m, lum(buf, w, bpp, xx, yy))
            line += '.' if m > 235 else ('-' if m > 180 else ('+' if m > 120 else '#'))
        print(line)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('png')
    ap.add_argument('--zoom', nargs=4, type=int, metavar=('X0', 'Y0', 'X1', 'Y1'))
    ap.add_argument('--runs', nargs=2, type=int, metavar=('Y0', 'Y1'))
    ap.add_argument('--col', nargs=3, type=int, metavar=('X', 'Y0', 'Y1'))
    args = ap.parse_args()
    try:
        w, h, bpp, buf = decode(args.png)
    except (ValueError, zlib.error, struct.error) as e:
        print(f'decode error: {e}', file=sys.stderr)
        return 2
    print(f'{w}x{h} bpp={bpp}')
    print('--- coarse map (whole image) ---')
    ascii_map(buf, w, h, bpp, 0, 0, w, h, max(1, w // 110), max(1, h // 44))
    if args.zoom:
        x0, y0, x1, y1 = args.zoom
        print(f'--- zoom {x0},{y0}..{x1},{y1} (2x2 px/char; each line prefixed with y) ---')
        for y in range(y0, min(y1, h), 2):
            line = f'{y:4d} '
            for x in range(x0, min(x1, w), 2):
                l = lum(buf, w, bpp, x, y)
                line += '.' if l > 235 else ('-' if l > 180 else ('+' if l > 120 else '#'))
            print(line)
    if args.runs:
        y0, y1 = args.runs
        print(f'--- dark runs (lum<120) per y in [{y0},{y1}) ---')
        for y in range(max(0, y0), min(y1, h)):
            runs = []
            start = None
            for x in range(w):
                dark = lum(buf, w, bpp, x, y) < 120
                if dark and start is None:
                    start = x
                if not dark and start is not None:
                    if x - start >= 2:
                        runs.append((start, x - 1))
                    start = None
            if start is not None:
                runs.append((start, w - 1))
            if runs:
                print(y, runs)
    if args.col:
        x, y0, y1 = args.col
        print(f'--- column {x} profile y in [{y0},{y1}) ---')
        for y in range(max(0, y0), min(y1, h)):
            i = (y * w + x) * bpp
            print(y, f'R{buf[i+2]} G{buf[i+1]} B{buf[i]}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
