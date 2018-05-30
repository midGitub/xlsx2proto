﻿using Aspose.Cells;
using System.IO;
using System.Collections.Generic;

namespace xlsx2proto
{
    class ExcelHelper
    {
        public static bool OpenWorkbook(out Workbook workbook, string path)
        {
            workbook = null;
            if (!File.Exists(path))
            {
                return false;
            }
            try
            {
                workbook = new Workbook(path);
            }
            catch (System.Exception ex)
            {
                workbook = null;
                Log.Error(ex);
                return false;
            }
            return true;
        }
        public static Workbook OpenWorkbook(string path)
        {
            Workbook workbook = null;
            OpenWorkbook(out workbook, path);
            return workbook;
        }
    }

    public enum TableType
    {
        Server,
        Client,
        Mix,
    }
    public enum FieldRequireType
    {
        Required,
        Optional,
        Repeated,
    }

    public class FieldInfo
    {
        public int col;
        public string language;
        public FieldRequireType requireType;
        public string type;
        public string name;


        public static bool CheckFieldName(string str)
        {
            if (str[0] > 47 && str[0] < 58) return false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                // 表示大写字母 A(65) - Z(90)
                if (c > 64 && c < 91)
                    continue;
                // 表示小写字母 a(97) - z(122)
                else if (c > 96 && c < 123)
                    continue;
                // 表示数字 0(48) - 9(57)
                else if (c > 47 && c < 58)
                    continue;
                // 表示下划线(95)
                else if (c == 95)
                    continue;
                else
                    return false;
            }
            return true;
        }
    }
    public class TableInfo
    {
        public string name;
        public TableType type;
        public Worksheet sheet;
        public List<FieldInfo> fields = new List<FieldInfo>();

        public TableInfo(Worksheet sheet, string name, TableType type)
        {
            this.sheet = sheet;
            this.name = name;
            this.type = type;

            Cells cells = sheet.Cells;
            for (int i = cells.MinColumn; i < cells.MaxColumn; i++)
            {
                string head = cells[0, i].StringValue;
                if (string.IsNullOrEmpty(head) || (!head.Contains("CS") && !head.Contains("CPP") && !head.Contains("LUA"))) continue; //三种语言都不需要
                string requireType = cells[1, i].StringValue.Trim().ToLower();
                if (string.IsNullOrEmpty(requireType))
                {
                    ShowError.RequireType("NULL", name, i);
                    continue;
                }
                FieldRequireType fieldRequireType = FieldRequireType.Optional;
                switch (requireType)
                {
                    case "required":
                        fieldRequireType = FieldRequireType.Required;
                        break;
                    case "optional":
                        fieldRequireType = FieldRequireType.Optional;
                        break;
                    case "repeated":
                        fieldRequireType = FieldRequireType.Repeated;
                        break;
                    default:
                        ShowError.RequireType(requireType, name, i);
                        continue;
                }
                string fieldType = cells[2, i].StringValue;
                ShowError.FieldType_ErrorMessageFmt = string.Format("{0}  ,在”{1}“的第 {2} 列", "{0}", name, i);
                if (!FieldType.ParseType(ref fieldType, fieldType)) continue;
                string fieldName = cells[3, i].StringValue.Trim();
                if (string.IsNullOrEmpty(fieldName))
                {
                    ShowError.FieldName("NULL", name, i);
                    continue;
                }
                if (!FieldInfo.CheckFieldName(fieldName))
                {
                    ShowError.FieldName(fieldName, name, i);
                    continue;
                }

                FieldInfo add = new FieldInfo();
                add.col = i;
                add.language = head;
                add.requireType = fieldRequireType;
                add.type = fieldType;
                add.name = fieldName;

                fields.Add(add);
            }
        }

        public static TableInfo Create(Worksheet sheet)
        {
            if (sheet == null) return null;
            string sheetName = sheet.Name.Trim();
            if (sheetName.StartsWith("S-") || sheetName.StartsWith("C-") || sheetName.StartsWith("M-"))
            {
                TableType tableType = TableType.Mix;
                if (sheetName.StartsWith("S-")) tableType = TableType.Server;
                else if (sheetName.StartsWith("C-")) tableType = TableType.Client;
                else if (sheetName.StartsWith("M-")) tableType = TableType.Mix;
                sheetName = sheetName.Remove(0, 2);
                if (string.IsNullOrEmpty(sheetName)) return null;
                return new TableInfo(sheet, sheetName, tableType);
            }
            else
            {
                return null;
            }
        }
    }
}
