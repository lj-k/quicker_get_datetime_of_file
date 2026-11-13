#!/usr/bin/env python3
# 把 Quicker 脚本包装成可独立编译的类（版本 0.01）
"""Quicker 会把脚本成员放入宿主类；本地测试时用同样的方式包装。"""

import sys
from pathlib import Path


def main():
    source_path = Path(sys.argv[1])
    output_path = Path(sys.argv[2])
    lines = source_path.read_text(encoding="utf-8").splitlines()

    usings = [line for line in lines if line.startswith("using ")]
    body = [line for line in lines if not line.startswith("using ")]

    text = "\n".join(usings) + "\n\npublic static class Script\n{\n" + "\n".join(body) + "\n}\n"
    output_path.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
