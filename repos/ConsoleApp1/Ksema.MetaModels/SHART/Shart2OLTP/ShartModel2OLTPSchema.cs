using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Ksema.MetaModels.OLTP;
using Shart.ModelDef;
using OLTPDataType=Ksema.MetaModels.OLTP.DataType;
using ShartDataType=Shart.ModelDef.DataType;

namespace Ksema.MetaModels.SHART.Shart2OLTP
{
    public class ShartModel2OLTPSchema
    {
        OLTPSchema _schema;
        IShartModelMetaData _shartModel;
        string _schemaOwner;
        private Dictionary<Column, IDimensionMetaData> _deferredReferences;


        public ShartModel2OLTPSchema(IShartModelMetaData aShartModel, string schemaOwner)
        {
            _shartModel = aShartModel;
            _schemaOwner = schemaOwner;
        }

        public static string GetReferenceFKName(Column foreignIdCol)
        {
            return foreignIdCol.Owner.Name + "_" + foreignIdCol.Name;
        }

        public OLTPSchema CreateOLTPSchema()
        {
            _schema = new OLTPSchema();
            _deferredReferences = new Dictionary<Column, IDimensionMetaData>();

            foreach (ICubeMetaData cube in _shartModel.GetAllCubesMetaData())
                CreateTablesForCube(cube);

            foreach (IDimensionMetaData dim in _shartModel.GetAllDimensionsMetaData())
                CreateTablesForDimension(dim);

            foreach (KeyValuePair<Column, IDimensionMetaData> entry in _deferredReferences)
                CreateFKForReference(entry.Key, entry.Value);

            return _schema;
        }

        private void CreateFKForReference(Column aColumn, IOLAPObjectMetaData aDim)
        {
            Table mainTable = _schema.GetTable(ShartMappingUtils.GetDimMainTableName(aDim));
            CreateFK4ID(mainTable, aColumn);
        }

        private void CreateTablesForCube(ICubeMetaData aCube)
        {
            Table viewTable = CreateTable(ShartMappingUtils.GetCubeViewTableName(aCube));

            CreateCubeViewTableStructure(aCube, viewTable);
        }

        private void CreateCubeViewTableStructure(IOLAPObjectMetaData aCube, Table aViewTable)
        {
			Column col;

            col = new Column();
            col.Name = ShartMappingUtils.GetCubeIDName(aCube);
            col.IsNullable = false;
            col.IsPK = true;
            col.DataType = GetOLTPDataType(ShartDataType.LongInteger);
            col.Size = 8;
            aViewTable.Columns.Add(col);

			col = new Column();
			col.Name = ShartMappingUtils.DateCreateName;
			col.IsNullable = false;
			col.IsPK = false;
			col.DataType = GetOLTPDataType(ShartDataType.DateTime);
			col.Size = 4;
			aViewTable.Columns.Add(col);

            CreateFieldsColumns(aCube.GetAllFields(), aViewTable);
        }

        private void CreateTablesForDimension(IDimensionMetaData aDim)
        {
            Table table = CreateTable(ShartMappingUtils.GetDimMainTableName(aDim));
            CreateDimMainTableStructure(aDim, table);

            Table viewTable = CreateTable(ShartMappingUtils.GetDimViewTableName(aDim));
            CreateDimViewTableStructure(aDim, viewTable, table);
        }

        private Table CreateTable(string tableName)
        {
            Table table = new Table();
            table.Schema = _schemaOwner;
            table.Name = tableName;
            _schema.Tables.Add(table);
            return table;
        }

        private void CreateDimViewTableStructure(IDimensionMetaData aDim, Table viewTable, Table mainTable)
        {
            Column col, idCol;

            col = new Column();
            col.Name = ShartMappingUtils.VersionIDName;
            col.IsNullable = false;
            col.IsPK = true;
            col.DataType = GetOLTPDataType(ShartDataType.Integer);
            col.Size = 4;
            viewTable.Columns.Add(col);

            idCol = col = new Column();
            col.Name = ShartMappingUtils.GetDimIDName(aDim);
            col.IsNullable = false;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.Integer);
            col.Size = 4;
            viewTable.Columns.Add(col);

            col = new Column();
            col.Name = "DateClose";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.DateTime);
            col.Size = 4;
            viewTable.Columns.Add(col);

            CreateDimensionViewBaseColumns(viewTable);

            CreateFieldsColumns(aDim.GetAllFields(), viewTable);

            foreach (IDataFieldMetaData dataField in aDim.GetDataFields())
            {
                if (dataField.IsAnalysis)
                {
                    for (int level = 1; level <= aDim.LevelCount; level++)
                    {
                        col = new Column();
                        col.Name = ShartMappingUtils.GetAnalysisColumnName(dataField.Name, level);
                        col.IsNullable = true;
                        col.IsPK = false;
                        col.DataType = GetOLTPDataType(dataField.DataType);
                        col.Size = dataField.DataSize;
                        viewTable.Columns.Add(col);
                    }
                }
            }

            CreateFK4ID(mainTable, idCol);
        }

        private void CreateFK4ID(Table primaryTable, Column foreignIdCol)
        {
            FK fk = new FK();
            fk.Id = GetReferenceFKName(foreignIdCol);
            FKColumn fkCol = new FKColumn();
            fkCol.Primary = primaryTable.GetPKColumns()[0];
            fkCol.Foreign = foreignIdCol;
            fk.FKColumns.Add(fkCol);
            foreignIdCol.Owner.FKeys.Add(fk);
        }


        private void CreateFieldsColumns(IEnumerable fields, Table table)
        {
            Column col;
            foreach (IFieldMetaData field in fields)
            {
				col = new Column();
				col.Name = field.Name;
				col.IsPK = false;
				col.DataType = GetOLTPDataType(field.DataType);
				col.Size = field.DataSize;
				col.IsNullable = true;
				col.Formula = field.Formula;
				table.Columns.Add(col);

				if (field is IReferenceMetaData)
				{
					IReferenceMetaData lRef = (IReferenceMetaData) field;
					col.IsNullable = !lRef.IsIdentifying;
					_deferredReferences.Add(col, lRef.Target);
				}
            }
        }

    	private void CreateDimensionViewBaseColumns(Table viewTable)
        {
            Column col;
            col = new Column();
            col.Name = "Guid";
            col.IsNullable = false;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(DbType.String);
            col.Size = 36;
            viewTable.Columns.Add(col);

            col = new Column();
            col.Name = "UserIDCreate";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.String);
            col.Size = 128;
            viewTable.Columns.Add(col);

            col = new Column();
            col.Name = "UserIDChange";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.String);
            col.Size = 128;
            viewTable.Columns.Add(col);

            col = new Column();
            col.Name = "DateCreate";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.DateTime);
            col.Size = 4;
            viewTable.Columns.Add(col);

            col = new Column();
            col.Name = "DateChange";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.DateTime);
            col.Size = 4;
            viewTable.Columns.Add(col);

        }

        private void CreateDimMainTableStructure(IDimensionMetaData aDim, Table aTable)
        {
            Column col;

            col = new Column();
            col.Name = ShartMappingUtils.GetDimIDName(aDim);
            col.IsNullable = false;
            col.IsPK = true;
            col.DataType = GetOLTPDataType(ShartDataType.Integer);
            col.Size = 4;
            aTable.Columns.Add(col);

            col = new Column();
            col.Name = "CurrLevel";
            col.IsNullable = false;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.Integer);
            col.Size = 4;
            aTable.Columns.Add(col);

            col = new Column();
            col.Name = "ParentID";
            col.IsNullable = true;
            col.IsPK = false;
            col.DataType = GetOLTPDataType(ShartDataType.Integer);
            col.Size = 4;
            aTable.Columns.Add(col);
            CreateFK4ID(aTable, col);

            for (int i = 0; i <= aDim.LevelCount; i++)
            {
                col = new Column();
                col.Name = ShartMappingUtils.GetParentColumnName(i);
                col.IsNullable = true;
                col.IsPK = false;
                col.DataType = GetOLTPDataType(ShartDataType.Integer);
                col.Size = 4;
                aTable.Columns.Add(col);

                CreateFK4ID(aTable, col);
            }
        }

        public static DbType GetDbTypeForShartType(ShartDataType shartType)
        {
            DbType type;
            switch (shartType)
            {
                case ShartDataType.Number:
                    type = DbType.Double;
                    break;
                case ShartDataType.String:
                    type = DbType.AnsiString;
                    break;
				case ShartDataType.Text:
					type = DbType.String;
					break;
				case ShartDataType.Boolean:
                    type = DbType.Boolean;
                    break;
                case ShartDataType.DateTime:
                    type = DbType.DateTime;
                    break;
                case ShartDataType.Integer:
                    type = DbType.Int32;
                    break;
                case ShartDataType.LongInteger:
                    type = DbType.Int64;
                    break;
                case ShartDataType.LongBinary:
                    type = DbType.Binary;
                    break;
				case ShartDataType.Image:
					type = DbType.Object;
					break;
				default:
                    throw new ArgumentOutOfRangeException("shartType");
            }

            return type;
        }

        private OLTPDataType GetOLTPDataType(ShartDataType shartType)
        {
            DbType type = GetDbTypeForShartType(shartType);
            return GetOLTPDataType(type);
        }

        private OLTPDataType GetOLTPDataType(DbType type)
        {
            foreach (OLTPDataType oltpType in _schema.Types)
            {
                if (oltpType.DBType == type)
                    return oltpType;
            }

            OLTPDataType newType = new OLTPDataType();
            newType.DBType = type;
            newType.Name = type.ToString();//TODO: придумать, откуда брать конкретное имя для БД
            _schema.Types.Add(newType);

            return newType;
        }

    }
}
