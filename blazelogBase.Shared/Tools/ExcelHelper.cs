using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Tools
{
    public static class ExcelHelper
    {

        static object mutex = new object();

        //To get the value of the cell, even it's empty. Unable to use loop by index
        private static string GetCellValue(WorkbookPart wbPart, List<Cell> theCells, string cellColumnReference)
        {
            Cell theCell = null;
            string value = "";
            foreach (Cell cell in theCells)
            {
                if (cell.CellReference.Value.StartsWith(cellColumnReference))
                {
                    theCell = cell;
                    break;
                }
            }
            if (theCell != null)
            {
                value = theCell.InnerText;
                // If the cell represents an integer number, you are done. 
                // For dates, this code returns the serialized value that represents the date. The code handles strings and 
                // Booleans individually. For shared strings, the code looks up the corresponding value in the shared string table. For Booleans, the code converts the value into the words TRUE or FALSE.
                if (theCell.DataType != null)
                {
                    switch (theCell.DataType.Value)
                    {
                        case CellValues.SharedString:
                            // For shared strings, look up the value in the shared strings table.
                            var stringTable = wbPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                            // If the shared string table is missing, something is wrong. Return the index that is in the cell. Otherwise, look up the correct text in the table.
                            if (stringTable != null)
                            {
                                value = stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                            }
                            break;
                        case CellValues.Boolean:
                            switch (value)
                            {
                                case "0":
                                    value = "FALSE";
                                    break;
                                default:
                                    value = "TRUE";
                                    break;
                            }
                            break;
                    }
                }
            }
            return value;
        }

        private static string GetCellValue(WorkbookPart wbPart, List<Cell> theCells, int index)
        {
            return GetCellValue(wbPart, theCells, GetExcelColumnName(index));
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;
            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }
            return columnName;
        }

        //Only xlsx files
        public static DataTable GetDataTableFromExcelFile(string filePath, string sheetName = "")
        {
            DataTable dt = new DataTable();
            try
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
                {
                    WorkbookPart wbPart = document.WorkbookPart;
                    IEnumerable<Sheet> sheets = document.WorkbookPart.Workbook.GetFirstChild<Sheets>().Elements<Sheet>();
                    string sheetId = sheetName != "" ? sheets.Where(q => q.Name == sheetName).First().Id.Value : sheets.First().Id.Value;
                    WorksheetPart wsPart = (WorksheetPart)wbPart.GetPartById(sheetId);
                    SheetData sheetdata = wsPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                    int totalHeaderCount = sheetdata.Descendants<Row>().ElementAt(0).Descendants<Cell>().Count();
                    //Get the header                    
                    for (int i = 1; i <= totalHeaderCount; i++)
                    {
                        dt.Columns.Add(GetCellValue(wbPart, sheetdata.Descendants<Row>().ElementAt(0).Elements<Cell>().ToList(), i));
                    }
                    foreach (Row r in sheetdata.Descendants<Row>())
                    {
                        if (r.RowIndex > 1)
                        {
                            DataRow tempRow = dt.NewRow();

                            //Always get from the header count, because the index of the row changes where empty cell is not counted
                            for (int i = 1; i <= totalHeaderCount; i++)
                            {
                                tempRow[i - 1] = GetCellValue(wbPart, r.Elements<Cell>().ToList(), i);
                            }
                            dt.Rows.Add(tempRow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return dt;
        }


        public static DataTable ListToDataTable<T>(List<T> list,bool onlyDisplay=false)
        {
            DataTable dt = new DataTable();
            var props = typeof(T).GetSortedProperties(onlyDisplay);
            foreach (var infop in props)
            {
                var info = infop.Key;
                var display = infop.Value;
                if (display == null || display.GetAutoGenerateField() == null || display.AutoGenerateField == true)
                {

                    if (display != null && !string.IsNullOrEmpty(display.GetName()))
                        dt.Columns.Add(new DataColumn(display.Name, GetNullableType(info.PropertyType)));
                    else
                        dt.Columns.Add(new DataColumn(infop.Key.Name.SplitCamelCase(), GetNullableType(info.PropertyType)));

                }


            }

            var getters = typeof(T).GetSortedGetters(onlyDisplay);
            foreach (T t in list)
            {
                DataRow row = dt.NewRow();
                int index = -1;
                foreach (var infop in props)
                {
                    var info = infop.Key;

                    var display = infop.Value;
                    if (display == null || display.GetAutoGenerateField() == null || display.AutoGenerateField == true)
                    {
                        index++;
                        if (!IsNullableType(info.PropertyType))
                            row[index] = getters[info.Name](t); //info.GetValue(t, null);
                        else
                            row[index] = (getters[info.Name](t) ?? DBNull.Value);
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
        private static Type GetNullableType(Type t)
        {
            Type returnType = t;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                returnType = Nullable.GetUnderlyingType(t);
            }
            return returnType;
        }
        private static bool IsNullableType(Type type)
        {
            return (type == typeof(string) ||
                    type.IsArray ||
                    (type.IsGenericType &&
                     type.GetGenericTypeDefinition().Equals(typeof(Nullable<>))));
        }

        static void SetCellwithValue(this XSSFWorkbook book, IRow fila, DataRow row, int iCol, DataColumn column)
        {
            //FORMATOS PARA CIERTOS TIPOS DE DATOS

            ICell cell = null; //<-Representa la celda actual                               
            object cellValue = row[iCol]; //<- El valor actual de la celda
            var my_font_1 = book.CreateFont();

            var _doubleCellStyle = book.CreateCellStyle();
            _doubleCellStyle.DataFormat = book.CreateDataFormat().GetFormat("#,##0.###");


            var _intCellStyle = book.CreateCellStyle();
            _intCellStyle.DataFormat = book.CreateDataFormat().GetFormat("#,##0");

            var _boolCellStyle = book.CreateCellStyle();
            _boolCellStyle.DataFormat = book.CreateDataFormat().GetFormat("BOOLEAN");

            var _dateCellStyle = book.CreateCellStyle();
            _dateCellStyle.DataFormat = book.CreateDataFormat().GetFormat("dd-MM-yyyy");

            var _dateTimeCellStyle = book.CreateCellStyle();
            _dateTimeCellStyle.DataFormat = book.CreateDataFormat().GetFormat("dd-MM-yyyy HH:mm:ss");

            var _dfCellStyle = book.CreateCellStyle();
            _dfCellStyle.WrapText = true;


            switch (column.DataType.ToString())
            {
                case "System.Boolean":
                    if (cellValue != DBNull.Value)
                    {

                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Boolean);

                        if (Convert.ToBoolean(cellValue)) { cell.SetCellFormula("TRUE()"); }
                        else { cell.SetCellFormula("FALSE()"); }

                        cell.CellStyle = _boolCellStyle;
                    }
                    break;

                case "System.String":
                    if (cellValue != DBNull.Value)
                    {

                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.String);
                        cell.SetCellValue((string)cellValue);
                        _dfCellStyle.VerticalAlignment = VerticalAlignment.Top;
                        cell.CellStyle = _dfCellStyle;



                    }
                    else
                    {

                    }
                    break;

                case "System.Int32":
                    if (cellValue != DBNull.Value)
                    {
                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Numeric);
                        cell.SetCellValue(Convert.ToInt32(cellValue));
                        cell.CellStyle = _intCellStyle;
                    }
                    break;
                case "System.Int64":
                    if (cellValue != DBNull.Value)
                    {
                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Numeric);
                        cell.SetCellValue(Convert.ToInt64(cellValue));
                        cell.CellStyle = _intCellStyle;

                    }
                    break;
                case "System.Decimal":
                    if (cellValue != DBNull.Value)
                    {
                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Numeric);
                        cell.SetCellValue(Convert.ToDouble(cellValue));
                        cell.CellStyle = _doubleCellStyle;
                    }
                    break;
                case "System.Double":
                    if (cellValue != DBNull.Value)
                    {
                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Numeric);
                        cell.SetCellValue(Convert.ToDouble(cellValue));
                        cell.CellStyle = _doubleCellStyle;
                    }
                    break;

                case "System.DateTime":
                    if (cellValue != DBNull.Value)
                    {
                        cell = fila.CreateCell(iCol, NPOI.SS.UserModel.CellType.Numeric);
                        cell.SetCellValue(Convert.ToDateTime(cellValue));

                        //Si No tiene valor de Hora, usar formato dd-MM-yyyy
                        DateTime cDate = Convert.ToDateTime(cellValue);
                        if (cDate != null && cDate.Hour > 0) { cell.CellStyle = _dateTimeCellStyle; }
                        else { cell.CellStyle = _dateCellStyle; }
                    }
                    break;
                default:
                    break;
            }


        }

        /// <summary>
        /// break the address into digit value
        /// </summary>
        /// <param name="address"></param>
        /// <returns>the row index is based on 1 (please minus 1 before using it as index)</returns>
        private static (string, int) ConvertAdress(string address)
        {
            var m = Regex.Match(address, @"\d+");
            if (m.Success)
            {
                var col = address.Substring(0, address.Length - m.Value.Length);
                return (col, int.Parse(m.Value));
            }
            return ("", 0);
        }
        public static IEnumerable<string> ReadExcel(string filename, params string[] locations)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                if (Path.GetExtension(filename) == ".xlsx")
                {
                    var workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0);

                    foreach (var lr in locations)
                    {
                        var l = lr;
                        var iswhole = l.StartsWith("*");
                        l = iswhole ? lr.Substring(1) : lr;
                        (var locol, var lorow) = ConvertAdress(l);
                        if (iswhole)
                        {
                            for (int i = lorow - 1; i < 1000; i++)
                            {
                                var row = sheet.GetRow(i);
                                var cell = new NPOI.SS.Util.CellReference($"{locol}{i}");
                                if (row == null)
                                    break;
                                var cellitem = row.GetCell(cell.Col);
                                if (cellitem == null)
                                    break;
                                if (string.IsNullOrEmpty(cellitem.StringCellValue))
                                    break;

                                yield return cellitem.StringCellValue;
                                if (!iswhole)
                                {
                                    break;
                                }
                            }
                        }

                    }


                }


            }



        }

        public static string CreateExcelFromDts(Dictionary<int, DataTable> sources, string templatefile, string targetfile)
        {
            lock (mutex)
            {
                try
                {
                    using (var excelObj = new ExcelPart.FastExcel(new FileInfo(templatefile), new FileInfo(targetfile)))
                    {
                        excelObj.Write(sources, "sheet1");
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    return string.Join("\n", ex.ToErrorMessageList());
                }
            }
        }


        public static string CreateExcelFromDt(DataTable source, string templatefile, string targetfile, bool useheading = true, int excount = 0)
        {
            lock (mutex)
            {
                try
                {
                    using (var excelObj = new ExcelPart.FastExcel(new FileInfo(templatefile), new FileInfo(targetfile)))
                    {
                        excelObj.Write(source, "sheet1", useheading, excount);
                    }
                    //var excel = new Microsoft.Office.Interop.Excel.Application();
                    //var wb = excel.Workbooks.Open(targetfile);
                    //var sheet = (Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[1];
                    //sheet.Activate();

                    //sheet.Cells.Style.WrapText = true;
                    //sheet.Cells.VerticalAlignment = Microsoft.Office.Interop.Excel.XlVAlign.xlVAlignTop;
                    //sheet.Cells.Columns.AutoFit();
                    //wb.Save();

                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
                    //Marshal.FinalReleaseComObject(sheet);

                    //wb.Close(Type.Missing, Type.Missing, Type.Missing);
                    //Marshal.FinalReleaseComObject(wb);

                    //excel.Quit();
                    //Marshal.FinalReleaseComObject(excel);

                    return "";
                }
                catch (Exception ex)
                {
                    return string.Join("\n", ex.ToErrorMessageList());
                }
            }



        }

        /// <summary>
        /// 根据DataTable 生成Excel
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static NpoiMemoryStream GetExcelFile(DataTable source)
        {

            //创建Excel文件的对象,XLSX 
            var book = new XSSFWorkbook();

            //添加一个sheet
            ISheet sheet = book.CreateSheet($"Result");

            //行下标记录
            int rowIndex = 0;
            //创建首行
            IRow row0 = sheet.CreateRow(rowIndex++);
            ////创建单元格
            //ICell cell0 = row0.CreateCell(0);
            ////设置单元格内容


            //cell0.CellStyle.Alignment = HorizontalAlignment.CenterSelection;

            //cell0.SetCellValue("料品情况查询");
            //sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, source.Columns.Count));

            // IRow row1 = sheet.CreateRow(rowIndex++);
            for (var i = 0; i < source.Columns.Count; i++)
            {

                var cell = row0.CreateCell(i, NPOI.SS.UserModel.CellType.String);
                cell.SetCellValue(source.Columns[i].ToString());

            }


            for (var i = 0; i < source.Rows.Count; i++)
            {

                IRow rowTemp = sheet.CreateRow(rowIndex++);

                for (var j = 0; j < source.Columns.Count; j++)
                {

                    if (source.Rows[i][j].GetType().Name == "Decimal")
                    {
                        rowTemp.CreateCell(j).SetCellValue(Convert.ToDouble(source.Rows[i][j]));
                    }
                    else
                    {
                        rowTemp.CreateCell(j).SetCellValue(source.Rows[i][j].ToString());
                    }



                }

            }

            var ms = new NpoiMemoryStream();
            ms.AllowClose = false;

            book.Write(ms);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.AllowClose = true;

            return ms;
        }

        /// <summary>
        /// 根据多个DataTable 生成一个Excel多个表
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static NpoiMemoryStream GetExcelFile(List<DataTable> sources)
        {

            //创建Excel文件的对象,XLSX 
            var book = new HSSFWorkbook();
            int flag = 1;
            foreach (DataTable source in sources)
            {
                //添加一个sheet
                ISheet sheet = book.CreateSheet($"Result");

                //行下标记录
                int rowIndex = 0;
                //创建首行
                IRow row0 = sheet.CreateRow(rowIndex++);
                ////创建单元格
                //ICell cell0 = row0.CreateCell(0);
                ////设置单元格内容


                //cell0.CellStyle.Alignment = HorizontalAlignment.CenterSelection;

                //cell0.SetCellValue("料品情况查询");
                //sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, source.Columns.Count));

                // IRow row1 = sheet.CreateRow(rowIndex++);
                for (var i = 0; i < source.Columns.Count; i++)
                {
                    row0.CreateCell(i).SetCellValue(source.Columns[i].ToString());
                }


                for (var i = 0; i < source.Rows.Count; i++)
                {

                    IRow rowTemp = sheet.CreateRow(rowIndex++);

                    for (var j = 0; j < source.Columns.Count; j++)
                    {

                        if (source.Rows[i][j].GetType().Name == "Decimal")
                        {
                            rowTemp.CreateCell(j).SetCellValue(Convert.ToDouble(source.Rows[i][j]));
                        }
                        else
                        {
                            rowTemp.CreateCell(j).SetCellValue(source.Rows[i][j].ToString());
                        }

                    }
                }
                for (int columnNum = 0; columnNum < source.Columns.Count; columnNum++)
                {
                    int columnWidth = (int)sheet.GetColumnWidth(columnNum) / 256;//获取当前列宽度  
                    for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)//在这一列上循环行  
                    {
                        IRow currentRow = sheet.GetRow(rowNum);
                        ICell currentCell = currentRow.GetCell(columnNum);
                        int length = Encoding.UTF8.GetBytes(currentCell.ToString()).Length;//获取当前单元格的内容宽度  
                        if (columnWidth < length + 1)
                        {
                            columnWidth = length + 1;
                        }//若当前单元格内容宽度大于列宽，则调整列宽为当前单元格宽度，后面的+1是我人为的将宽度增加一个字符  
                    }
                    //sheet.SetColumnWidth(columnNum, columnWidth * 256);
                    if (columnWidth > 255)
                    {
                        columnWidth = 254;
                    }
                    else
                    {
                        sheet.SetColumnWidth(columnNum, columnWidth * 256);
                    }

                }
                flag++;
            }
            var ms = new NpoiMemoryStream();
            ms.AllowClose = false;
            book.Write(ms);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            ms.AllowClose = true;

            return ms;
        }
        /// <summary>
        /// 重写Npoi方法
        /// </summary>
        public class NpoiMemoryStream : MemoryStream
        {
            public NpoiMemoryStream()
            {
                AllowClose = true;
            }

            public bool AllowClose { get; set; }

            public override void Close()
            {
                if (AllowClose)
                    base.Close();
            }
        }

        public static DataTable ReadToDataTableByNPOI(Stream streamfile, string ext)
        {

            DataTable dt = new DataTable();

            IWorkbook workbook = null;// Create a workbook to install Excel table
            if (ext == ".xls".ToLower()) //filePath
            {//.xls
                #region  .xls file processing: HSSFWorkbook

                workbook = new HSSFWorkbook(streamfile);
                ISheet sheet = workbook.GetSheetAt(0);  // Get the first worksheet (sheet)
                System.Collections.IEnumerator rows = sheet.GetRowEnumerator();
                HSSFRow headerRow = (HSSFRow)sheet.GetRow(0);

                // line of the last checkered number that is the total number of columns 
                for (int j = 0; j < (sheet.GetRow(0).LastCellNum); j++)
                {
                    //SET EVERY COLUMN NAME
                    HSSFCell cell = (HSSFCell)headerRow.GetCell(j);

                    dt.Columns.Add(cell.ToString());
                }

                while (rows.MoveNext())
                {
                    IRow row = (HSSFRow)rows.Current;
                    DataRow dr = dt.NewRow();

                    if (row.RowNum == 0) continue;//The firt row is title,no need import

                    for (int i = 0; i < row.LastCellNum; i++)
                    {
                        if (i >= dt.Columns.Count)// cell count> column count, then break // record the number of cells per field is not greater than the number table 20140213
                        {
                            break;
                        }

                        ICell cell = row.GetCell(i);



                        if (cell == null)
                        {
                            dr[i] = null;
                        }
                        else
                        {
                            //dr[i] = cell.ToString();
                            if (cell.CellType == NPOI.SS.UserModel.CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                            {
                                dr[i] = cell.DateCellValue?.ToString("yyyy-MM-dd hh:mm:ss"); // // If the content is converted into this format time format
                            }
                            else
                            {
                                dr[i] = row.GetCell(i).ToString();
                            }
                        }
                    }

                    dt.Rows.Add(dr);
                }
                #endregion
            }
            else
            {//.xlsx
                #region  .xlsx file processing: XSSFWorkbook

                workbook = new XSSFWorkbook(streamfile);
                ISheet sheet = workbook.GetSheetAt(0);
                System.Collections.IEnumerator rows = sheet.GetRowEnumerator();
                XSSFRow headerRow = (XSSFRow)sheet.GetRow(0);



                // line of the last checkered number that is the total number of columns 
                for (int j = 0; j < (sheet.GetRow(0).LastCellNum); j++)
                {
                    //SET EVERY COLUMN NAME
                    XSSFCell cell = (XSSFCell)headerRow.GetCell(j);

                    dt.Columns.Add(cell.ToString());

                }

                while (rows.MoveNext())
                {
                    IRow row = (XSSFRow)rows.Current;
                    DataRow dr = dt.NewRow();

                    if (row.RowNum == 0) continue;//The firt row is title,no need import

                    for (int i = 0; i < row.LastCellNum; i++)
                    {
                        if (i >= dt.Columns.Count)// cell count> column count, then break // record the number of cells per field is not greater than the number table 20140213
                        {
                            break;
                        }

                        ICell cell = row.GetCell(i);


                        if (cell == null)
                        {
                            dr[i] = null;
                        }
                        else
                        {
                            //dr[i] = cell.ToString();
                            if (cell.CellType == NPOI.SS.UserModel.CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                            {
                                dr[i] = cell.DateCellValue?.ToString("yyyy-MM-dd hh:mm:ss"); // If the content is converted into this format time format
                            }
                            else
                            {
                                dr[i] = row.GetCell(i).ToString();
                            }
                        }
                    }
                    dt.Rows.Add(dr);
                }
                #endregion
            }
            return dt;
        }


    }
}
