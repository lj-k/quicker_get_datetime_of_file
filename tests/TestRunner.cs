// 文件日期提取脚本的回归测试（版本 0.01）
using System;
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
        ExpectCreationFallback();
        ExpectSkip("文件夹跳过", root);
        ExpectSkip("不存在文件跳过", Path.Combine(root, "not-exists.jpg"));
        ExpectThrow("全部关闭且无文件日期", Path.Combine(root, "plain.txt"));

        Console.WriteLine();
        Console.WriteLine("通过：" + passed + "，失败：" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static TestContext Run(string path, int minYear, bool matchFile, bool matchExif, bool matchModified, bool matchCreated)
    {
        TestContext context = new TestContext();
        context.Set("输入文件路径", path);
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

            if (!(context.Get("yyyy") is int) || !(context.Get("mm") is int) || !(context.Get("dd") is int))
            {
                Fail(name, "yyyy/mm/dd 不是 int");
                return;
            }
            if ((int)context.Get("yyyy") != actual.Year || (int)context.Get("mm") != actual.Month || (int)context.Get("dd") != actual.Day)
            {
                Fail(name, "yyyy/mm/dd 与 date 不一致");
                return;
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
