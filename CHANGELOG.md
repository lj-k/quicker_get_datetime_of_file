# Changelog

## 0.01（2026-11-13）

- 初始版本。
- 新增文件名日期匹配：`yyyy-mm-dd` 与 `yyyymmdd`，支持年份范围、前置非数字边界、格式1优先。
- 新增 EXIF 拍摄日期读取：JPEG/TIFF、大小端、`DateTimeOriginal` 与 `DateTimeDigitized`。
- 新增修改日期、创建日期回退。
- 新增文件夹与无效路径跳过。
- 新增可导入 Quicker 动作 JSON，包含多字段表单与「运行C#代码」步骤。
- 新增 24 项回归测试。
