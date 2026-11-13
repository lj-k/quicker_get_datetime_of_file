#!/usr/bin/env python3
# 构建 Quicker 动作 JSON
"""把 src/FileDateExtractor.cs 打包为可导入 Quicker 的动作 JSON。"""

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CS_PATH = ROOT / "src" / "FileDateExtractor.cs"
OUT_PATH = ROOT / "dist" / "FileDateExtractor.quicker.json"

ACTION_ID = "6f1c2a7e-3b4d-4f8a-9c21-7d5e8a0b4c63"
VERSION = "0.02"

TEXT = 0
NUMBER = 1
BOOLEAN = 2
DATETIME = 6
INTEGER = 12
OBJECT = 98


def variable(key, var_type, desc, default_value):
    return {
        "Key": key,
        "Type": var_type,
        "Desc": desc,
        "DefaultValue": default_value,
        "SaveState": False,
        "IsInput": False,
        "IsOutput": False,
        "ParamName": None,
        "InputParamInfo": None,
        "OutputParamInfo": None,
        "TableDef": None,
        "CustomType": None,
        "Group": None,
    }


def form_field(key, label, help_text, input_method, **overrides):
    field = {
        "FieldKey": key,
        "DictVarType": None,
        "Label": label,
        "HelpText": help_text,
        "HelpLink": None,
        "InputMethod": input_method,
        "SelectionItems": "",
        "IsRequired": False,
        "MinValue": "",
        "MaxValue": "",
        "Pattern": "",
        "InputWidth": "",
        "MaxLength": 0,
        "ImeState": None,
        "TextTools": "",
        "ExtraSettings": "",
        "VisibleExpression": "",
        "DefaultValue": None,
        "ColumnWidth": 0.0,
        "OnlyDate": False,
        "Group": "",
    }
    field.update(overrides)
    return field


def param(value):
    return {"VarKey": None, "Value": value}


def build():
    script = CS_PATH.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\n", "\r\n")

    variables = [
        variable("输入文件路径", TEXT, "单个文件路径，或多行路径列表（每行一个）；文件夹会被跳过", ""),
        variable("最早起始年份", INTEGER, "文件名日期匹配的最早年份，范围到当前年", "1900"),
        variable("匹配文件名日期", BOOLEAN, "是否从文件名中提取日期", "true"),
        variable("匹配exif拍摄日期", BOOLEAN, "是否读取 EXIF 拍摄日期", "true"),
        variable("匹配修改日期", BOOLEAN, "是否使用文件修改日期", "true"),
        variable("匹配创建日期", BOOLEAN, "是否使用文件创建日期", "true"),
        variable("date", OBJECT, "单个输入时为日期时间；列表输入时为日期文本列表（yyyy-MM-dd HH:mm:ss）", ""),
        variable("yyyy", OBJECT, "单个输入时为四位年份文本；列表输入时为文本列表", ""),
        variable("mm", OBJECT, "单个输入时为两位月份文本；列表输入时为文本列表", ""),
        variable("dd", OBJECT, "单个输入时为两位日期文本；列表输入时为文本列表", ""),
        variable("yyyy_MM", OBJECT, "yyyy.MM 格式文本（如 2020.02）；列表输入时为文本列表", ""),
        variable("formOk", BOOLEAN, "表单是否确认", "false"),
        variable("scriptOk", BOOLEAN, "C# 脚本是否执行成功", "false"),
    ]

    fields = [
        form_field(
            "输入文件路径",
            "输入文件路径",
            "单个路径，或每行一个路径（列表）；文件夹会自动跳过",
            2,
            IsRequired=False,
        ),
        form_field(
            "最早起始年份",
            "最早起始年份",
            "仅用于文件名日期；有效范围：该年份到当前年",
            7,
            MinValue="1900",
            MaxValue="9999",
        ),
        form_field("匹配文件名日期", "匹配文件名日期", "支持 yyyy-mm-dd、yyyymmdd", 6),
        form_field(
            "匹配exif拍摄日期",
            "匹配 EXIF 拍摄日期",
            "读取 DateTimeOriginal，缺失时读 DateTimeDigitized",
            6,
        ),
        form_field("匹配修改日期", "匹配修改日期", "使用文件的最后修改时间", 6),
        form_field("匹配创建日期", "匹配创建日期", "使用文件的创建时间", 6),
    ]

    form_def = json.dumps({"Fields": fields}, ensure_ascii=False, separators=(",", ":"))

    form_step = {
        "StepRunnerKey": "sys:form",
        "InputParams": {
            "operation": param("variables"),
            "title": param("文件日期提取"),
            "formDef": param(form_def),
            "titleColumnWidth": param("160"),
            "windowWidth": param("560"),
            "windowHeight": param("0"),
            "restoreFocus": param("1"),
            "topMost": param("true"),
            "disableEnterSubmit": param("0"),
            "stopIfFail": param("1"),
            "defaultInputWidth": param("0"),
        },
        "OutputParams": {"isSuccess": "formOk", "button": None, "selectedGroup": None},
        "IfSteps": None,
        "ElseSteps": None,
        "Note": "",
        "Disabled": False,
        "Collapsed": False,
        "DelayMs": 0,
    }

    script_step = {
        "StepRunnerKey": "sys:csscript",
        "InputParams": {
            "mode": param("normal_roslyn"),
            "script": param(script),
            "reference": param(""),
            "enableCache": param("true"),
            "runOnUiThread": param("background"),
            "stopIfFail": param("true"),
        },
        "OutputParams": {"isSuccess": "scriptOk", "rtn": None, "errMessage": None},
        "IfSteps": None,
        "ElseSteps": None,
        "Note": "",
        "Disabled": False,
        "Collapsed": False,
        "DelayMs": 0,
    }

    data = {
        "LimitSingleInstance": True,
        "SummaryExpression": "$$",
        "SubPrograms": [],
        "Variables": variables,
        "Steps": [form_step, script_step],
    }

    now = datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")

    action = {
        "Row": 0,
        "Col": 0,
        "ActionType": 24,
        "Title": "文件日期提取",
        "Description": "支持单个路径或路径列表，按文件名 → EXIF 拍摄日期 → 修改日期 → 创建日期的顺序提取文件日期，自适应输出单个值或列表。版本 " + VERSION + "。",
        "Icon": "fa:Solid_CalendarDay",
        "Path": None,
        "DelayMs": 0,
        "Data": json.dumps(data, ensure_ascii=False, separators=(",", ":")),
        "Data2": None,
        "Data3": None,
        "Children": None,
        "Id": ACTION_ID,
        "TemplateId": None,
        "TemplateRevision": 0,
        "UseTemplate": False,
        "LastEditTimeUtc": now,
        "SharedActionId": None,
        "ShareTimeUtc": None,
        "CreateTimeUtc": now,
        "AsSubProgram": False,
        "SkipWhenStopRunningActions": False,
        "SkipCheckUpdate": False,
        "AutoUpdate": False,
        "KeepInfoWhenUpdate": False,
        "MinQuickerVersion": "",
        "ContextMenuData": "",
        "AllowScrollTrigger": False,
        "EnableEvaluateVariable": False,
        "IsTextProcessor": False,
        "IsImageProcessor": False,
        "Association": {
            "MatchProcess": None,
            "IsImageProcessor": False,
            "ReturnImageFromFirstScreenShotStep": True,
            "IsTextProcessor": False,
            "ReturnTextFromGetSelectedTextStep": True,
            "TextMatchExpression": "",
            "TextMinLength": 0,
            "TextMaxLength": 0,
            "IsHtmlProcessor": False,
            "IsFileProcessor": False,
            "FileMinCount": 0,
            "FileMaxCount": 0,
            "AllowedFileExtensions": "",
            "RequireAllFileMatchExt": False,
            "SearchBoxPlaceholder": "",
            "IsWindowProcessor": False,
            "EnableRealtimeSearch": False,
            "BrowserContextMenu": None,
            "UrlPattern": "",
        },
        "DoNotClosePanel": False,
        "UserLimitation": None,
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(action, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return script


def verify(script):
    action = json.loads(OUT_PATH.read_text(encoding="utf-8"))
    assert action["ActionType"] == 24
    assert action["Id"] == ACTION_ID
    assert VERSION in action["Description"]

    data = json.loads(action["Data"])
    assert len(data["Variables"]) == 13
    keys = [v["Key"] for v in data["Variables"]]
    assert len(keys) == len(set(keys)), "变量名重复"
    assert "yyyy_MM" in keys, "缺少 yyyy_MM 变量"

    assert len(data["Steps"]) == 2
    form_step, script_step = data["Steps"]
    assert form_step["StepRunnerKey"] == "sys:form"
    assert script_step["StepRunnerKey"] == "sys:csscript"
    assert script_step["InputParams"]["script"]["Value"] == script

    form_def = json.loads(form_step["InputParams"]["formDef"]["Value"])
    assert len(form_def["Fields"]) == 6
    form_keys = [f["FieldKey"] for f in form_def["Fields"]]
    assert set(form_keys).issubset(set(keys)), "表单字段缺少对应变量"

    assert "public static void Exec(Quicker.Public.IStepContext context)" in script
    assert "context.SetVarValue(\"date\", singleDate);" in script
    assert "context.SetVarValue(\"yyyy_MM\"" in script
    assert "context.SetVarValue(\"date\", dates);" in script

    print("OK:", OUT_PATH.relative_to(ROOT))
    print("变量:", len(keys), "步骤:", len(data["Steps"]), "表单字段:", len(form_keys))


if __name__ == "__main__":
    verify(build())
