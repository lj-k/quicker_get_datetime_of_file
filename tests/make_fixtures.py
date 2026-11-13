#!/usr/bin/env python3
# 生成回归测试用的文件与 EXIF 样本（版本 0.01）
"""生成 tests 用的临时文件，全部写入命令行指定的目录。"""

import os
import struct
import sys


def jpeg_with_exif(path, date_text, big_endian=False, tag=0x9003):
    if big_endian:
        header = b"MM\x00\x2a" + struct.pack(">I", 8)
        u16 = lambda v: struct.pack(">H", v)
        u32 = lambda v: struct.pack(">I", v)
    else:
        header = b"II\x2a\x00" + struct.pack("<I", 8)
        u16 = lambda v: struct.pack("<H", v)
        u32 = lambda v: struct.pack("<I", v)

    # IFD0：一条 ExifIFDPointer，指向紧随其后的 Exif IFD（偏移 26）
    ifd0 = u16(1) + u16(0x8769) + u16(4) + u32(1) + u32(26) + u32(0)

    # Exif IFD：一条 ASCII 日期，字符串放在 IFD 之后（偏移 44）
    text = date_text.encode("ascii") + b"\x00"
    exif_ifd = u16(1) + u16(tag) + u16(2) + u32(len(text)) + u32(44) + u32(0)

    tiff = header + ifd0 + exif_ifd + text
    payload = b"Exif\x00\x00" + tiff
    app1 = b"\xff\xe1" + struct.pack(">H", len(payload) + 2) + payload
    with open(path, "wb") as f:
        f.write(b"\xff\xd8" + app1 + b"\xff\xd9")


def main():
    root = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else "tests/fixtures")
    os.makedirs(root, exist_ok=True)

    plain = ["2020-02-02_photo.jpg", "IMG20200202.jpg", "120200202.jpg", "2019-02-02.jpg",
             "2021-05-06_20200202.jpg", "2020-02-02_exif.jpg", "plain.txt",
             "bad_month_2020-13-01.jpg", "bad_day_2020-02-30.jpg", "x2020-02-02y.jpg",
             "2020-2-2.jpg", "photo.png"]
    for name in plain:
        with open(os.path.join(root, name), "wb") as f:
            f.write(b"fixture")

    jpeg_with_exif(os.path.join(root, "photo_exif_le.jpg"), "2022:06:07 08:09:10")
    jpeg_with_exif(os.path.join(root, "photo_exif_be.jpg"), "2023:07:08 09:10:11", big_endian=True)
    jpeg_with_exif(os.path.join(root, "photo_exif_digitized.jpg"), "2024:08:09 10:11:12",
                   tag=0x9004)

    sub = os.path.join(root, "sub", "2020-02-02")
    os.makedirs(sub, exist_ok=True)
    with open(os.path.join(sub, "inside.jpg"), "wb") as f:
        f.write(b"fixture")

    stamp = 1614744306  # 2021-03-03 04:05:06 UTC
    for dirpath, _, filenames in os.walk(root):
        for name in filenames:
            os.utime(os.path.join(dirpath, name), (stamp, stamp))

    print("fixtures:", root)


if __name__ == "__main__":
    main()
