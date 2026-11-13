#!/usr/bin/env bash
# 编译并运行回归测试（版本 0.02）
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUILD="$ROOT/.test-build"

rm -rf "$BUILD"
mkdir -p "$BUILD"

python3 "$ROOT/tests/make_fixtures.py" "$BUILD/fixtures"
python3 "$ROOT/tests/wrap_script.py" "$ROOT/src/FileDateExtractor.cs" "$BUILD/ScriptWrapped.cs"

mcs -langversion:5 -out:"$BUILD/tests.exe" \
    "$BUILD/ScriptWrapped.cs" \
    "$ROOT/tests/QuickerStub.cs" \
    "$ROOT/tests/TestRunner.cs"

mono "$BUILD/tests.exe" "$BUILD/fixtures"
