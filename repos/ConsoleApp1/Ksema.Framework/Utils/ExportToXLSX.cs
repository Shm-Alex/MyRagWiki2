using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Framework.Utils;
using Ksema.Framework.Diagnostics;
using Ksema.Framework.Tasks;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;


namespace Ksema.Framework.Utils
{
	public class ExportToXLSX : IDisposable
	{

		public void Dispose()
		{
			
		}
		
		public XSSFRow CreateRow(ISheet sheet, int rowNum, float rowHeight = 56)
		{
			XSSFRow row = (XSSFRow)sheet.CreateRow(rowNum); // Create line
			row.HeightInPoints = rowHeight; // Set column header row height
			return row;
		}

		private ISheet ActiveSheet = null;
		public ISheet getActiveSheet() => ActiveSheet;
		private IWorkbook workbook = null;
		private List<string> sheetsNames = new List<string>();

		private string _ActiveSheetName;
		public string ActiveSheetName {
			get => _ActiveSheetName;

			set
			{
				if (workbook.NumberOfSheets > 0)
				{
					var sheetIds = Enumerable.Range(0, workbook.NumberOfSheets);
					sheetsNames = sheetIds.Select(x => workbook.GetSheetAt(x).SheetName)
										.ToList();
				}

				if (sheetsNames.Contains(value))
				{
					ActiveSheet = workbook.GetSheet(value);
				}
				else
				{
					/*("Нельзя выбрать не существующий лист в книге.");*/
				}
				_ActiveSheetName = value;
			}
		}
		public int tid = 0;
		public int ColumnLastUsedIndex = 10;
		private TempFileToTargetFileStruct ttf { get; set; }

		public static Dictionary<long, TaskExportTickProcesInfo> tid_rn = new Dictionary<long, TaskExportTickProcesInfo>();

		public ExportToXLSX(string Titles, List<string> SheetsName, TempFileToTargetFileStruct ttf, string ActiveSheetName = "")
		{
			if (workbook == null && SheetsName.Any())
			{
				int tid = (int) DateTime.Now.Ticks;
				ExportToXLSX.tid_rn.Add(tid, new TaskExportTickProcesInfo());
				this.ttf = ttf;
				this.tid = tid;
				workbook = new XSSFWorkbook();
				foreach (var sheetName in SheetsName)
				{
					workbook.CreateSheet(sheetName);
				}

				_ActiveSheetName = SheetsName.First();

				var ids = SheetsName.Select((x, index) => new Tuple<string, int>(x, index)).ToDictionary(x=>x.Item1,y=>y.Item2);

				ActiveSheet = string.IsNullOrEmpty(ActiveSheetName) 
									? workbook.GetSheetAt(ids[SheetsName.FirstOrDefault()]) 
									: workbook.GetSheetAt(ids[ActiveSheetName]);

				
				InitTitleAndFormaters(Titles.Split(';').ToList());
			}

		}
		private readonly Dictionary<CellStlEnum, ICellStyle> CellStyles = new Dictionary<CellStlEnum, ICellStyle>();

		public ICellStyle getCellStle(CellStlEnum styNm) => CellStyles.ContainsKey(styNm) ? CellStyles[styNm] : null;


		public enum CellStlEnum
		{
			defaultStyle,
			boldCentredStyle,
			centredWrapStyle,
			wrapStyle
		}

		private bool isInitStyles = false;
		private void InitCellStyles()
		{
			IFont font = workbook.CreateFont();
			font.IsBold = true;
			font.FontHeightInPoints = 11;
			var boldCentredStyle = workbook.CreateCellStyle();
			boldCentredStyle.SetFont(font);
			boldCentredStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
			boldCentredStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

			CellStyles.Add(CellStlEnum.boldCentredStyle, boldCentredStyle);

			IFont font2 = workbook.CreateFont();
			font.FontHeightInPoints = 11;
			var CentredWrapStyle = workbook.CreateCellStyle();
			CentredWrapStyle.SetFont(font2);
			CentredWrapStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
			CentredWrapStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
			CentredWrapStyle.WrapText = true;

			CellStyles.Add(CellStlEnum.centredWrapStyle, CentredWrapStyle);

			var WrapStyle = workbook.CreateCellStyle();
			WrapStyle.SetFont(font);
			WrapStyle.WrapText = true;

			CellStyles.Add(CellStlEnum.wrapStyle, WrapStyle);

			var dateFormat = workbook.CreateDataFormat();
			var dataStyle = workbook.CreateCellStyle();
			dataStyle.DataFormat = dateFormat.GetFormat("yyyy-MM-dd HH:mm:ss");

			isInitStyles = true;

		}

		public void InitTitleAndFormaters(List<string> Titles)
		{
			if(!isInitStyles)
				InitCellStyles();
			var FirstRowCels = new List<string>().ToList();
			for(var i = 0;i<= Titles.Count();i++)
				FirstRowCels.Add("");
			AppendRow(FirstRowCels,CellStlEnum.boldCentredStyle);
			AppendRow(Titles, CellStlEnum.boldCentredStyle);
			ColumnLastUsedIndex = Titles.Count();
			ActiveSheet.SetAutoFilter(new CellRangeAddress(2, 2, 0, Titles.Count()-1));
		}

		public int IncrementRow(int tid )
		{
			if(!tid_rn[tid].SeetUsedRow.ContainsKey(ActiveSheetName))
				tid_rn[tid].SeetUsedRow.Add(ActiveSheetName, 0);
			
			var id = tid_rn[tid].SeetUsedRow[ActiveSheetName];
			tid_rn[tid].SeetUsedRow[ActiveSheetName] = Interlocked.Increment(ref id);
			return tid_rn[tid].SeetUsedRow[ActiveSheetName];
		}

		public void AutosizeColumns(string skipColNum = "", int DefLength = 10000)
		{
			var colNoAutoSize = global::Framework.Utils.Utils.RowToIntList(skipColNum);
			for (var i = 0; i <= ColumnLastUsedIndex - 1 ; i++)
				if (colNoAutoSize.Contains(i))
					ActiveSheet.SetColumnWidth(i, DefLength);
				else
					ActiveSheet.AutoSizeColumn(i);
		}
		public void HideSpecifiedColumns(string HideColNumList)
		{
			var cols4Hide = global::Framework.Utils.Utils.RowToIntList(HideColNumList);
			foreach (var colNum in cols4Hide)
				ActiveSheet.SetColumnHidden(colNum, true);
		}

		public void AddGrouping()
		{
			var rcnt = ActiveSheet.PhysicalNumberOfRows;
			string prevCode = "Parent";
			for (int i = 1; i <= rcnt; i++)
			{
				var cell = ActiveSheet.GetRow(i).GetCell(0);
				var cVal = cell.StringCellValue;
				if (!tid_rn[tid].ParentGuidIntervals.ContainsKey(cVal))
					tid_rn[tid].ParentGuidIntervals.Add(cVal, new int[] { tid_rn[tid].SeetUsedRow[ActiveSheetName], tid_rn[tid].SeetUsedRow[ActiveSheetName] });

				if (cVal != prevCode)
				{
					tid_rn[tid].ParentGuidIntervals[cVal] = new int[] { i, i };
				}
				else
				{
					tid_rn[tid].ParentGuidIntervals[cVal][1] = i;
				}

				prevCode = cVal;
			}

			tid_rn[tid].ParentGuidIntervals.Remove("Parent");

			foreach (var diap in tid_rn[tid].ParentGuidIntervals.Select(x => x.Value))
				ActiveSheet.GroupRow(diap[0] + 1, diap[1]);
		}

		public void MergeTopHeader(int RowStart = 1,int c = 1, int ColStart = 1, int ColFinish = 11)
		{
			//Merge the cell
			CellRangeAddress region = new CellRangeAddress(RowStart, ColStart, ColStart, ColFinish);
			ActiveSheet.AddMergedRegion(region);
		}


		public void UpdateCell(string cellValue, int rowId, int colId, CellStlEnum cellStyle = CellStlEnum.defaultStyle)
		{
			try
			{
				XSSFRow row = (XSSFRow)ActiveSheet.GetRow(rowId);
				var cell = row.GetCell(colId);

				cell.SetCellValue(cellValue.Length > 32767 ? cellValue.Substring(0, 32767) : cellValue);
				cell.SetCellType(CellType.String);
				if (cellStyle != CellStlEnum.defaultStyle)
					cell.CellStyle = CellStyles[cellStyle];
			}
			catch
			{
				/**/
			}
		}

		public void AppendRow(IEnumerable<string> rowVals, CellStlEnum cellStyle = CellStlEnum.defaultStyle)
		{
			try
			{
				XSSFRow row = CreateRow(ActiveSheet, IncrementRow(tid));
				ICell cell;
				int i = 0;
				
				foreach (var x in rowVals)
				{
					cell = row.CreateCell(i);
					cell.SetCellValue(x.Length > 32767 ? x.Substring(0, 32767) : x);
					cell.SetCellType(CellType.String);
					if (cellStyle != CellStlEnum.defaultStyle)
						cell.CellStyle = CellStyles[cellStyle];

					i++;
				}
			}
			catch(Exception ex)
			{
				/**/
			}
		}

		public void Save(string exclAutoSizeColList = "", string hidenColNums = "", bool isGrouping = false)
		{
			try
			{
				using (FileStream stream = new FileStream(ttf.targetfile, FileMode.Create, FileAccess.Write))
				{	
					AutosizeColumns(exclAutoSizeColList);

					if (isGrouping)
						AddGrouping();

					HideSpecifiedColumns(hidenColNums);

					if (sheetsNames.Count > 1)
					{
						foreach (var shetName in sheetsNames.Where(x=>x != ActiveSheetName).ToList())
						{
							ActiveSheetName = shetName;
							AutosizeColumns(exclAutoSizeColList);
						}
					}
					
					workbook.Write(stream);
				}
			}
			catch (Exception ex)
			{
				Tracer.Error(ex);
			    throw ex;
			}
		}

		public string MoveAfterSave(string tickId, int cntL0, int cnt,   Dictionary<int, List<string>> FailRow_nbrs = null)
		{
		    if (cnt == 0)
			{
				 return TryMove(ttf.statusFile, ttf.TempTickIdPath, ttf.TargetTickPath, Path.GetFileName(ttf.targetfile),
									DateTime.Now.ToString("HH-mm-ss") + "_Пусто", $"{tickId}:");
			}
			else
			{
			    if (FailRow_nbrs != null && FailRow_nbrs.Any())
			    {
			        AppendRow(FailRow_nbrs.Select(kv => $"{kv.Key} - {kv.Value}"), CellStlEnum.centredWrapStyle);
			    }

			    return TryMove(ttf.statusFile, ttf.TempTickIdPath, ttf.TargetTickPath, Path.GetFileName(ttf.targetfile),
									DateTime.Now.ToString("HH-mm-ss") + $"_("+global::Framework.Utils.Utils.numEnding(cntL0)+")"+(cnt != cntL0 ? $"~{cnt}" : ""), $"{tickId}:");
				// не показываем число внутренних задач если количество L0 = общему числу задач.
			}
		}

		public string TryMove(string statusFilePath, string fromPath, string targetTickPath, string fileName, string altName, string replaces)
		{
		    if (string.IsNullOrEmpty(targetTickPath))
		        return fileName;

			var from = Path.Combine(fromPath, fileName);
			var to = Path.Combine(targetTickPath, fileName);
			var movedTarget = getTargetXlsxFiles(to, altName, replaces);
			try
			{
                File.Move(from, movedTarget);
			}
			catch
			{
				File.Copy(from, movedTarget);
			}
		    return movedTarget;
		}

		private static string getTargetXlsxFiles(string targetfile, string AltName, string Replaces)
		{
			var res = targetfile.Substring(0, targetfile.Length - 4) + "_" + AltName + ".xlsx";
			var repl = Replaces.Split('|');
			foreach (var rs in repl)
			{
				var kv = rs.Split(':');
				res = res.Replace(kv[0], kv.Length > 1 ? kv[1] : "");
			}
			return res;
		}
	}
}
