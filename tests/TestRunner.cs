// 文件日期提取脚本的回归测试（版本 0.02）
using System;
using System.Collections.Generic;
using System.IO;
using Quicker.Public;

public static class TestRunner
{
    private static int passed;
    private static int failed;
    private static string root;

    public static int Main(string[] args)
    {
        root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath("tests/fixtures");
        Console.WriteLine("测试目录：" + root);

        ExpectDate("文件名 yyyy-mm-dd", "2020-02-02_photo.jpg", 1900, true, true, true, true, new DateTime(2020, 2, 2));
        ExpectDate("文件名 yyyymmdd", "IMG20200202.jpg", 1900, true, true, true, true, new DateTime(2020, 2, 2));
        ExpectDate("前置数字不提取", "120200202.jpg", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("年份超出范围回退", "2019-02-02.jpg", 2020, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("格式1优先于格式2", "2021-05-06_20200202.jpg", 1900, true, true, true, true, new DateTime(2021, 5, 6));
        ExpectDate("EXIF 小端拍摄日期", "photo_exif_le.jpg", 1900, true, true, true, true, new DateTime(2022, 6, 7));
        ExpectDate("文件名优先于 EXIF", "2020-02-02_exif.jpg", 1900, true, true, true, true, new DateTime(2020, 2, 2));
        ExpectDate("修改日期兜底", "plain.txt", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("非法月份回退", "bad_month_2020-13-01.jpg", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("非法日期回退", "bad_day_2020-02-30.jpg", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("前后非数字可匹配", "x2020-02-02y.jpg", 1900, true, true, true, true, new DateTime(2020, 2, 2));
        ExpectDate("非两位月日不匹配", "2020-2-2.jpg", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("EXIF 大端拍摄日期", "photo_exif_be.jpg", 1900, true, true, true, true, new DateTime(2023, 7, 8));
        ExpectDate("EXIF 数字化日期兜底", "photo_exif_digitized.jpg", 1900, true, true, true, true, new DateTime(2024, 8, 9));
        ExpectDate("非图片无 EXIF", "photo.png", 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("目录日期不参与", Path.Combine("sub", "2020-02-02", "inside.jpg"), 1900, true, true, true, true, new DateTime(2021, 3, 3));
        ExpectDate("关闭 EXIF 回退修改日期", "photo_exif_le.jpg", 1900, true, false, true, true, new DateTime(2021, 3, 3));
        ExpectDate("关闭文件名后使用 EXIF", "photo_exif_le.jpg", 1900, false, true, true, true, new DateTime(2022, 6, 7));

        ExpectExact("EXIF 保留时分秒", "photo_exif_le.jpg", new DateTime(2022, 6, 7, 8, 9, 10));
        ExpectQuotedPath();
        ExpectSingleTextWithNewline();
        ExpectCreationFallback();
        ExpectSkip("文件夹跳过", root);
        ExpectSkip("不存在文件跳过", Path.Combine(root, "not-exists.jpg"));
        ExpectThrow("单个文件全部关闭且无文件日期", Path.Combine(root, "plain.txt"));

        ExpectList("列表三个文件",
            new List<string>
            {
                Path.Combine(root, "2020-02-02_photo.jpg"),
                Path.Combine(root, "IMG20200202.jpg"),
                Path.Combine(root, "plain.txt")
            },
            new string[] { "2020", "2020", "2021" },
            new string[] { "02", "02", "03" },
            new string[] { "02", "02", "03" },
            new string[] { "2020.02", "2020.02", "2021.03" });

        ExpectList("列表跳过文件夹和缺失项",
            new List<string>
            {
                root,
                Path.Combine(root, "not-exists.jpg"),
                Path.Combine(root, "2020-02-02_photo.jpg")
            },
            new string[] { "2020" },
            new string[] { "02" },
            new string[] { "02" },
            new string[] { "2020.02" });

        ExpectList("多行文本按列表输出",
            Path.Combine(root, "2020-02-02_photo.jpg") + "\r\n" + Path.Combine(root, "IMG20200202.jpg"),
            new string[] { "2020", "2020" },
            new string[] { "02", "02" },
            new string[] { "02", "02" },
            new string[] { "2020.02", "2020.02" });

        ExpectList("单元素列表仍输出列表",
            new List<string> { Path.Combine(root, "photo_exif_le.jpg") },
            new string[] { "2022" },
            new string[] { "06" },
            new string[] { "07" },
            new string[] { "2022.06" });

        ExpectList("列表非法日期回退",
            new List<string>
            {
                Path.Combine(root, "bad_month_2020-13-01.jpg"),
                Path.Combine(root, "plain.txt")
            },
            new string[] { "2021", "2021" },
            new string[] { "03", "03" },
            new string[] { "03", "03" },
            new string[] { "2021.03", "2021.03" });

        ExpectList("列表全部无效输出空列表",
            new List<string> { root, Path.Combine(root, "not-exists.jpg") },
            new string[0], new string[0], new string[0], new string[0]);

        ExpectList("空列表输出空列表",
            new List<string>(),
            new string[0], new string[0], new string[0], new string[0]);

        Console.WriteLine();
        Console.WriteLine("通过：" + passed + "，失败：" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static TestContext Run(object pathValue, int minYear, bool matchFile, bool matchExif, bool matchModified, bool matchCreated)
    {
        TestContext context = new TestContext();
        context.Set("输入文件路径", pathValue);
        context.Set("最早起始年份", minYear);
        context.Set("匹配文件名日期", matchFile);
        context.Set("匹配exif拍摄日期", matchExif);
        context.Set("匹配修改日期", matchModified);
        context.Set("匹配创建日期", matchCreated);
        Script.Exec(context);
        return context;
    }

    private static void ExpectDate(string name, string fileName, int minYear, bool matchFile, bool matchExif,
        bool matchModified, bool matchCreated, DateTime expected)
    {
        try
        {
            TestContext context = Run(Path.Combine(root, fileName), minYear, matchFile, matchExif, matchModified, matchCreated);
            object value = context.Get("date");
            if (!(value is DateTime))
            {
                Fail(name, "date 不是 DateTime：" + Describe(value));
                return;
            }

            DateTime actual = (DateTime)value;
            if (actual.Year != expected.Year || actual.Month != expected.Month || actual.Day != expected.Day)
            {
                Fail(name, "期望 " + expected.ToString("yyyy-MM-dd") + "，实际 " + actual.ToString("yyyy-MM-dd"));
                return;
            }

            if (!(context.Get("yyyy") is string) || !(context.Get("mm") is string) ||
                !(context.Get("dd") is string) || !(context.Get("yyyy_MM") is string))
            {
                Fail(name, "yyyy/mm/dd/yyyy_MM 不是文本");
                return;
            }

            if ((string)context.Get("yyyy") != actual.ToString("yyyy") ||
                (string)context.Get("mm") != actual.ToString("MM") ||
                (string)context.Get("dd") != actual.ToString("dd") ||
                (string)context.Get("yyyy_MM") != actual.ToString("yyyy.MM"))
            {
                Fail(name, "文本输出与 date 不一致");
                return;
            }

            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectList(string name, object inputValue, string[] expectedYears, string[] expectedMonths,
        string[] expectedDays, string[] expectedYearMonths)
    {
        try
        {
            TestContext context = Run(inputValue, 1900, true, true, true, true);
            List<string> dates = context.Get("date") as List<string>;
            List<string> years = context.Get("yyyy") as List<string>;
            List<string> months = context.Get("mm") as List<string>;
            List<string> days = context.Get("dd") as List<string>;
            List<string> yearMonths = context.Get("yyyy_MM") as List<string>;

            if (dates == null || years == null || months == null || days == null || yearMonths == null)
            {
                Fail(name, "输出不是 List<string>");
                return;
            }

            if (dates.Count != expectedYears.Length || years.Count != expectedYears.Length ||
                months.Count != expectedYears.Length || days.Count != expectedYears.Length ||
                yearMonths.Count != expectedYears.Length)
            {
                Fail(name, "输出数量不一致");
                return;
            }

            for (int i = 0; i < expectedYears.Length; i++)
            {
                if (years[i] != expectedYears[i] || months[i] != expectedMonths[i] ||
                    days[i] != expectedDays[i] || yearMonths[i] != expectedYearMonths[i])
                {
                    Fail(name, "第 " + (i + 1) + " 项文本不一致");
                    return;
                }

                string prefix = expectedYears[i] + "-" + expectedMonths[i] + "-" + expectedDays[i];
                if (!dates[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    Fail(name, "date 列表元素格式不正确：" + dates[i]);
                    return;
                }
            }

            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectExact(string name, string fileName, DateTime expected)
    {
        try
        {
            TestContext context = Run(Path.Combine(root, fileName), 1900, true, true, true, true);
            object value = context.Get("date");
            if (!(value is DateTime) || (DateTime)value != expected)
            {
                Fail(name, "期望 " + expected.ToString("yyyy-MM-dd HH:mm:ss") + "，实际 " + Describe(value));
                return;
            }
            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectQuotedPath()
    {
        try
        {
            string path = "\"" + Path.Combine(root, "2020-02-02_photo.jpg") + "\"";
            TestContext context = Run(path, 1900, true, true, true, true);
            object value = context.Get("date");
            if (!(value is DateTime) || ((DateTime)value).Date != new DateTime(2020, 2, 2))
            {
                Fail("带引号的路径", "实际 " + Describe(value));
                return;
            }
            Pass("带引号的路径");
        }
        catch (Exception ex)
        {
            Fail("带引号的路径", "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectSingleTextWithNewline()
    {
        try
        {
            string input = Path.Combine(root, "2020-02-02_photo.jpg") + "\r\n";
            TestContext context = Run(input, 1900, true, true, true, true);
            object value = context.Get("date");
            if (!(value is DateTime) || ((DateTime)value).Date != new DateTime(2020, 2, 2))
            {
                Fail("单路径尾随换行按单值输出", "实际 " + Describe(value));
                return;
            }
            Pass("单路径尾随换行按单值输出");
        }
        catch (Exception ex)
        {
            Fail("单路径尾随换行按单值输出", "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectCreationFallback()
    {
        try
        {
            TestContext context = Run(Path.Combine(root, "plain.txt"), 1900, false, false, false, true);
            if (!(context.Get("date") is DateTime))
            {
                Fail("创建日期兜底", "date 不是 DateTime：" + Describe(context.Get("date")));
                return;
            }
            Pass("创建日期兜底");
        }
        catch (Exception ex)
        {
            Fail("创建日期兜底", "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectSkip(string name, string path)
    {
        try
        {
            TestContext context = Run(path, 1900, true, true, true, true);
            if (context.Get("date") != null)
            {
                Fail(name, "不应输出 date：" + Describe(context.Get("date")));
                return;
            }
            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, "异常 " + ex.GetType().Name + "：" + ex.Message);
        }
    }

    private static void ExpectThrow(string name, string path)
    {
        try
        {
            Run(path, 1900, false, false, false, false);
            Fail(name, "应当抛出 InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            Pass(name);
        }
        catch (Exception ex)
        {
            Fail(name, "异常类型错误：" + ex.GetType().Name);
        }
    }

    private static string Describe(object value)
    {
        if (value == null)
        {
            return "null";
        }
        return value.ToString() + " (" + value.GetType().Name + ")";
    }

    private static void Pass(string name)
    {
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static void Fail(string name, string message)
    {
        failed++;
        Console.WriteLine("FAIL " + name + " - " + message);
    }
}
