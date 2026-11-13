// 文件日期提取脚本（Quicker「运行C#代码」普通模式）
// 版本：0.02

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static void Exec(Quicker.Public.IStepContext context)
{
    bool isList;
    List<string> paths = GetInputPaths(context.GetVarValue("输入文件路径"), out isList);

    int minYear = ToInt(context.GetVarValue("最早起始年份"), 1900);
    bool matchFileName = ToBool(context.GetVarValue("匹配文件名日期"), true);
    bool matchExif = ToBool(context.GetVarValue("匹配exif拍摄日期"), true);
    bool matchModified = ToBool(context.GetVarValue("匹配修改日期"), true);
    bool matchCreated = ToBool(context.GetVarValue("匹配创建日期"), true);

    if (!isList)
    {                                        
        if (paths.Count == 0)
        {
            return;
        }

        string filePath = paths[0];
        if (!File.Exists(filePath))
        {
            return;
        }

        DateTime singleDate;
        if (!TryGetFileDate(filePath, minYear, matchFileName, matchExif, matchModified, matchCreated, out singleDate))
        {
            throw new InvalidOperationException("未能从文件获取日期：" + filePath);
        }

        context.SetVarValue("date", singleDate);
        context.SetVarValue("yyyy", singleDate.ToString("yyyy", CultureInfo.InvariantCulture));
        context.SetVarValue("mm", singleDate.ToString("MM", CultureInfo.InvariantCulture));
        context.SetVarValue("dd", singleDate.ToString("dd", CultureInfo.InvariantCulture));
        context.SetVarValue("yyyy_MM", singleDate.ToString("yyyy.MM", CultureInfo.InvariantCulture));
        return;
    }

    List<string> dates = new List<string>();
    List<string> years = new List<string>();
    List<string> months = new List<string>();
    List<string> days = new List<string>();
    List<string> yearMonths = new List<string>();

    for (int i = 0; i < paths.Count; i++)
    {
        string filePath = paths[i];
        if (!File.Exists(filePath))
        {
            continue;
        }

        DateTime date;
        if (!TryGetFileDate(filePath, minYear, matchFileName, matchExif, matchModified, matchCreated, out date))
        {
            continue;
        }

        dates.Add(date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        years.Add(date.ToString("yyyy", CultureInfo.InvariantCulture));
        months.Add(date.ToString("MM", CultureInfo.InvariantCulture));
        days.Add(date.ToString("dd", CultureInfo.InvariantCulture));
        yearMonths.Add(date.ToString("yyyy.MM", CultureInfo.InvariantCulture));
    }

    context.SetVarValue("date", dates);
    context.SetVarValue("yyyy", years);
    context.SetVarValue("mm", months);
    context.SetVarValue("dd", days);
    context.SetVarValue("yyyy_MM", yearMonths);
}

private static List<string> GetInputPaths(object value, out bool isList)
{
    isList = false;
    List<string> paths = new List<string>();

    if (value == null)
    {
        return paths;
    }

    string text = value as string;
    if (text != null)
    {
        string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            if (lines.Length == 1)
            {
                string singlePath = NormalizePath(lines[0]);
                if (singlePath.Length > 0)
                {
                    paths.Add(singlePath);
                }
            }
            return paths;
        }

        isList = true;
        for (int i = 0; i < lines.Length; i++)
        {
            string linePath = NormalizePath(lines[i]);
            if (linePath.Length > 0)
            {
                paths.Add(linePath);
            }
        }
        return paths;
    }

    IEnumerable enumerable = value as IEnumerable;
    if (enumerable != null && !(value is IDictionary))
    {
        isList = true;
        foreach (object item in enumerable)
        {
            string itemPath = NormalizePath(ToText(item));
            if (itemPath.Length > 0)
            {
                paths.Add(itemPath);
            }
        }
        return paths;
    }

    string fallbackPath = NormalizePath(ToText(value));
    if (fallbackPath.Length > 0)
    {
        paths.Add(fallbackPath);
    }
    return paths;
}

private static bool TryGetFileDate(string filePath, int minYear, bool matchFileName, bool matchExif,
    bool matchModified, bool matchCreated, out DateTime date)
{
    date = DateTime.MinValue;
    bool found = false;

    // 1. 文件名中的日期
    if (!found && matchFileName)
    {
        found = TryGetDateFromFileName(filePath, minYear, out date);
    }

    // 2. EXIF 拍摄日期
    if (!found && matchExif)
    {
        found = TryGetExifTakenDate(filePath, out date);
    }

    // 3. 修改日期
    if (!found && matchModified)
    {
        try
        {
            date = File.GetLastWriteTime(filePath);
            found = date != DateTime.MinValue;
        }
        catch
        {
            found = false;
        }
    }

    // 4. 创建日期
    if (!found && matchCreated)
    {
        try
        {
            date = File.GetCreationTime(filePath);
            found = date != DateTime.MinValue;
        }
        catch
        {
            found = false;
        }
    }

    return found;
}

private static string NormalizePath(string path)
{
    if (path == null)
    {
        return "";
    }

    string value = path.Trim();
    if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
    {
        value = value.Substring(1, value.Length - 2).Trim();
    }

    if (value.Length == 0)
    {
        return "";
    }

    try
    {
        value = Environment.ExpandEnvironmentVariables(value);
        return Path.GetFullPath(value);
    }
    catch
    {
        return value;
    }
}

private static string ToText(object value)
{
    if (value == null)
    {
        return "";
    }
    return Convert.ToString(value, CultureInfo.InvariantCulture);
}

private static int ToInt(object value, int defaultValue)
{
    if (value == null)
    {
        return defaultValue;
    }

    try
    {
        if (value is int)
        {
            return (int)value;
        }
        if (value is long)
        {
            return (int)(long)value;
        }
        if (value is short)
        {
            return (int)(short)value;
        }
        if (value is double)
        {
            double number = (double)value;
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                return defaultValue;
            }
            return (int)Math.Round(number, MidpointRounding.AwayFromZero);
        }
        if (value is decimal)
        {
            return (int)Math.Round((decimal)value, MidpointRounding.AwayFromZero);
        }
        if (value is bool)
        {
            return ((bool)value) ? 1 : 0;
        }

        int parsed;
        string text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }
    }
    catch
    {
    }

    return defaultValue;
}

private static bool ToBool(object value, bool defaultValue)
{
    if (value == null)
    {
        return defaultValue;
    }
    if (value is bool)
    {
        return (bool)value;
    }

    try
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        if (text.Length == 0)
        {
            return defaultValue;
        }
        if (text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "是" || text == "真")
        {
            return true;
        }
        if (text == "0" || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) || text == "否" || text == "假")
        {
            return false;
        }

        bool parsed;
        if (bool.TryParse(text, out parsed))
        {
            return parsed;
        }
    }
    catch
    {
    }

    return defaultValue;
}

private static bool TryGetDateFromFileName(string filePath, int minYear, out DateTime result)
{
    result = DateTime.MinValue;

    string fileName;
    try
    {
        fileName = Path.GetFileNameWithoutExtension(filePath);
    }
    catch
    {
        return false;
    }

    if (string.IsNullOrEmpty(fileName))
    {
        return false;
    }

    int maxYear = DateTime.Now.Year;

    // 格式1：yyyy-mm-dd。日期前必须是非数字字符，或位于字符串起始。
    MatchCollection matches = Regex.Matches(fileName, @"(?<![0-9])([0-9]{4})-([0-9]{2})-([0-9]{2})");
    for (int i = 0; i < matches.Count; i++)
    {
        Match match = matches[i];
        int year = ParseInt(match.Groups[1].Value);
        int month = ParseInt(match.Groups[2].Value);
        int day = ParseInt(match.Groups[3].Value);
        if (TryBuildDate(year, month, day, minYear, maxYear, out result))
        {
            return true;
        }
    }

    // 格式2：yyyymmdd。日期前必须是非数字字符，或位于字符串起始。
    matches = Regex.Matches(fileName, @"(?<![0-9])([0-9]{8})");
    for (int i = 0; i < matches.Count; i++)
    {
        Match match = matches[i];
        string text = match.Groups[1].Value;
        int year = ParseInt(text.Substring(0, 4));
        int month = ParseInt(text.Substring(4, 2));
        int day = ParseInt(text.Substring(6, 2));
        if (TryBuildDate(year, month, day, minYear, maxYear, out result))
        {
            return true;
        }
    }

    result = DateTime.MinValue;
    return false;
}

private static int ParseInt(string text)
{
    int value;
    if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
    {
        return value;
    }
    return -1;
}

private static bool TryBuildDate(int year, int month, int day, int minYear, int maxYear, out DateTime result)
{
    result = DateTime.MinValue;

    if (year < minYear || year > maxYear)
    {
        return false;
    }
    if (month < 1 || month > 12)
    {
        return false;
    }
    if (day < 1 || day > 31)
    {
        return false;
    }

    try
    {
        result = new DateTime(year, month, day);
        return true;
    }
    catch
    {
        result = DateTime.MinValue;
        return false;
    }
}

private static bool TryGetExifTakenDate(string filePath, out DateTime result)
{
    result = DateTime.MinValue;

    try
    {
        FileInfo info = new FileInfo(filePath);
        if (info.Length <= 0 || info.Length > 268435456L)
        {
            return false;
        }

        byte[] data = File.ReadAllBytes(filePath);
        int tiffStart;
        int tiffLength;
        if (!TryLocateExifTiff(data, out tiffStart, out tiffLength))
        {
            return false;
        }

        return TryReadExifDateFromTiff(data, tiffStart, tiffLength, out result);
    }
    catch
    {
        result = DateTime.MinValue;
        return false;
    }
}

private static bool TryLocateExifTiff(byte[] data, out int tiffStart, out int tiffLength)
{
    tiffStart = 0;
    tiffLength = 0;

    if (data == null || data.Length < 8)
    {
        return false;
    }

    // TIFF：II*\0 或 MM\0*
    if ((data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) ||
        (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A))
    {
        tiffStart = 0;
        tiffLength = data.Length;
        return true;
    }

    // JPEG：遍历段，查找 APP1 中的 Exif\0\0
    if (data[0] != 0xFF || data[1] != 0xD8)
    {
        return false;
    }

    int offset = 2;
    while (offset + 4 <= data.Length)
    {
        if (data[offset] != 0xFF)
        {
            offset++;
            continue;
        }

        byte marker = data[offset + 1];
        if (marker == 0xFF)
        {
            offset++;
            continue;
        }
        if (marker == 0x00 || marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
        {
            offset += 2;
            continue;
        }
        if (marker == 0xDA || marker == 0xD9)
        {
            break;
        }

        int segmentLength = (data[offset + 2] << 8) | data[offset + 3];
        if (segmentLength < 2 || offset + 2 + segmentLength > data.Length)
        {
            break;
        }

        if (marker == 0xE1 && segmentLength >= 8)
        {
            int payload = offset + 4;
            if (data[payload] == 0x45 && data[payload + 1] == 0x78 && data[payload + 2] == 0x69 &&
                data[payload + 3] == 0x66 && data[payload + 4] == 0x00 && data[payload + 5] == 0x00)
            {
                tiffStart = payload + 6;
                tiffLength = segmentLength - 8;
                return tiffLength >= 8;
            }
        }

        offset += 2 + segmentLength;
    }

    return false;
}

private static bool TryReadExifDateFromTiff(byte[] data, int tiffStart, int tiffLength, out DateTime result)
{
    result = DateTime.MinValue;

    if (tiffLength < 8)
    {
        return false;
    }

    bool littleEndian;
    if (data[tiffStart] == 0x49 && data[tiffStart + 1] == 0x49)
    {
        littleEndian = true;
    }
    else if (data[tiffStart] == 0x4D && data[tiffStart + 1] == 0x4D)
    {
        littleEndian = false;
    }
    else
    {
        return false;
    }

    if (ReadUInt16(data, tiffStart + 2, littleEndian) != 42)
    {
        return false;
    }

    uint ifd0Offset = ReadUInt32(data, tiffStart + 4, littleEndian);
    uint exifIfdOffset;
    bool hasExifIfd = TryFindIfdUInt32(data, tiffStart, tiffLength, ifd0Offset, 0x8769, littleEndian, out exifIfdOffset);

    if (hasExifIfd)
    {
        string text;

        // DateTimeOriginal：拍摄日期
        if (TryFindIfdAscii(data, tiffStart, tiffLength, exifIfdOffset, 0x9003, littleEndian, out text) &&
            TryParseExifDateTime(text, out result))
        {
            return true;
        }

        // DateTimeDigitized：数字化日期，部分相机没有 DateTimeOriginal 时用它兜底
        if (TryFindIfdAscii(data, tiffStart, tiffLength, exifIfdOffset, 0x9004, littleEndian, out text) &&
            TryParseExifDateTime(text, out result))
        {
            return true;
        }
    }

    result = DateTime.MinValue;
    return false;
}

private static bool TryGetIfdEntries(byte[] data, int tiffStart, int tiffLength, uint ifdOffset, bool littleEndian,
    out int entryCount, out int entriesStart)
{
    entryCount = 0;
    entriesStart = 0;

    if (ifdOffset < 8 || ifdOffset > (uint)(tiffLength - 2))
    {
        return false;
    }

    int position = tiffStart + (int)ifdOffset;
    if (position + 2 > tiffStart + tiffLength)
    {
        return false;
    }

    int count = ReadUInt16(data, position, littleEndian);
    if (count > 4096)
    {
        return false;
    }

    entriesStart = position + 2;
    if (entriesStart + count * 12 > tiffStart + tiffLength)
    {
        return false;
    }

    entryCount = count;
    return true;
}

private static bool TryFindIfdUInt32(byte[] data, int tiffStart, int tiffLength, uint ifdOffset, int wantedTag,
    bool littleEndian, out uint value)
{
    value = 0;

    int entryCount;
    int entriesStart;
    if (!TryGetIfdEntries(data, tiffStart, tiffLength, ifdOffset, littleEndian, out entryCount, out entriesStart))
    {
        return false;
    }

    for (int i = 0; i < entryCount; i++)
    {
        int entry = entriesStart + i * 12;
        if (entry + 12 > tiffStart + tiffLength)
        {
            return false;
        }

        int tag = ReadUInt16(data, entry, littleEndian);
        if (tag != wantedTag)
        {
            continue;
        }

        int type = ReadUInt16(data, entry + 2, littleEndian);
        uint count = ReadUInt32(data, entry + 4, littleEndian);
        if (type != 4 || count != 1)
        {
            return false;
        }

        value = ReadUInt32(data, entry + 8, littleEndian);
        return true;
    }

    return false;
}

private static bool TryFindIfdAscii(byte[] data, int tiffStart, int tiffLength, uint ifdOffset, int wantedTag,
    bool littleEndian, out string value)
{
    value = null;

    int entryCount;
    int entriesStart;
    if (!TryGetIfdEntries(data, tiffStart, tiffLength, ifdOffset, littleEndian, out entryCount, out entriesStart))
    {
        return false;
    }

    for (int i = 0; i < entryCount; i++)
    {
        int entry = entriesStart + i * 12;
        if (entry + 12 > tiffStart + tiffLength)
        {
            return false;
        }

        int tag = ReadUInt16(data, entry, littleEndian);
        if (tag != wantedTag)
        {
            continue;
        }

        int type = ReadUInt16(data, entry + 2, littleEndian);
        uint count = ReadUInt32(data, entry + 4, littleEndian);
        if (type != 2 || count == 0 || count > 1024)
        {
            return false;
        }

        int valuePosition;
        if (count <= 4)
        {
            valuePosition = entry + 8;
        }
        else
        {
            uint offset = ReadUInt32(data, entry + 8, littleEndian);
            if (offset >= (uint)tiffLength)
            {
                return false;
            }
            valuePosition = tiffStart + (int)offset;
        }

        if (valuePosition + (int)count > tiffStart + tiffLength)
        {
            return false;
        }

        string text = Encoding.ASCII.GetString(data, valuePosition, (int)count);
        text = text.TrimEnd('\0', ' ');
        if (text.Length == 0)
        {
            return false;
        }

        value = text;
        return true;
    }

    return false;
}

private static bool TryParseExifDateTime(string text, out DateTime result)
{
    result = DateTime.MinValue;

    if (string.IsNullOrEmpty(text))
    {
        return false;
    }

    string value = text.Trim().TrimEnd('\0').Trim();
    if (value.Length < 10)
    {
        return false;
    }

    string[] formats = new string[]
    {
        "yyyy:MM:dd HH:mm:ss",
        "yyyy:MM:dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy:MM:dd",
        "yyyy-MM-dd"
    };

    if (!DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
    {
        result = DateTime.MinValue;
        return false;
    }

    if (result.Year < 1900)
    {
        result = DateTime.MinValue;
        return false;
    }

    return true;
}

private static ushort ReadUInt16(byte[] data, int offset, bool littleEndian)
{
    if (littleEndian)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
    return (ushort)((data[offset] << 8) | data[offset + 1]);
}

private static uint ReadUInt32(byte[] data, int offset, bool littleEndian)
{
    if (littleEndian)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
    return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
}
