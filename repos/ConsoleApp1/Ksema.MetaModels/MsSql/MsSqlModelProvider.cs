using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Shart.ModelDef;
using Ksema.Framework.Database;
using System.Data;
using System.IO;
using Framework.Compress;
using Shart.Service.Model;
using System.Xml;

namespace Shart.ModelUpdater.MsSql
{
	public class MsSqlModelProvider : IModelProvider
	{
		private IDatabase _db;

		public MsSqlModelProvider( )
		{
		}

		public MsSqlModelProvider( IDatabase db )
		{
			_db = db;
		}

		public IEnumerable<string> GetModelsList(  )
		{
			List<string> result = new List<string>();
			string sql = "SELECT Name FROM Models";
			using( DbConnectionScope cs = DbConnectionScope.Create( _db ) )
			{
				IDbCommand cmd = cs.CreateCommand( sql );
				using( IDataReader reader = cmd.ExecuteReader() )
				{
					while( reader.Read() )
					{
						result.Add( reader.GetString(0) );
					}
				}
			}
			return result;
		}

		public void SaveModel( ShartModelMetaData model )
		{
			StringBuilder builder = new StringBuilder();
			using( StringWriter writer = new StringWriter( builder ) )
			{
				ShartModelMetaDataSerializer.Save( writer, model );
			}
			byte[] data = Framework.Compress.Serializer.toBytes( builder.ToString() );
			data = Compressor.Compress( data );
			string sql = "IF EXISTS (SELECT * FROM Models WHERE Name=@modelName)" + Environment.NewLine
				+ "	UPDATE Models SET Data = @modelData, ChangeDate = @date WHERE Name=@modelName" + Environment.NewLine
				+ " ELSE " + Environment.NewLine
				+ "	INSERT INTO Models( Name, Data, ChangeDate) VALUES( @modelName, @modelData, @date )";

			using( DbConnectionScope cs = DbConnectionScope.Create( _db ) )
			{
				IDbCommand cmd = cs.CreateCommand( sql );

				var namePar = cmd.CreateParameter();
				namePar.DbType = DbType.String;
				namePar.ParameterName = "@modelName";
				namePar.Value = model.Name;
				cmd.Parameters.Add( namePar );

				cmd.AddParameter("@date", DateTime.Now, DbType.DateTime);

				var dataPar = cmd.CreateParameter();
				dataPar.ParameterName = "@modelData";
				dataPar.DbType = DbType.Binary;
				dataPar.Value = data;
				cmd.Parameters.Add( dataPar );

				cmd.ExecuteNonQuery();
			}
		}

		public IShartModelMetaData LoadModel( string name )
		{
			IShartModelMetaData result = null;
			string sql = "SELECT Data FROM Models WHERE Name=@modelName";
			byte[] modelData;
			using( DbConnectionScope cs = DbConnectionScope.Create( _db ) )
			{
				IDbCommand cmd = cs.CreateCommand( sql );
				var namePar = cmd.CreateParameter();
				namePar.ParameterName = "@modelName";
				namePar.Value = name;
				cmd.Parameters.Add( namePar );

				modelData = (byte[])cmd.ExecuteScalar();
			}
			if( modelData != null )
			{
				modelData = Compressor.Decompress(modelData);
				string modelString = Framework.Compress.Serializer.fromBytes(modelData);
				result = (ShartModelMetaData)ShartModelMetaDataSerializer.LoadFromString( modelString );
			}
			return result;
		}

		public DateTime GetModelChangeDate(IEnumerable<string> models)
		{
			DateTime result = DateTime.MaxValue;
			using (var scope = DbConnectionScope.Create(_db))
			{
				var command = scope.CreateCommand(String.Empty);
				string commandText = String.Format("select max(changeDate) from [dbo].[Models] where Name in ({0})", DBCommonUtils.CreateXmlParam(command, "@models", "varchar(255)", models));
				command.CommandText = commandText;

				var value = command.ExecuteScalar();
				if (value != null && value != DBNull.Value)
					result = (DateTime)value;
			}
			return result;
		}
	

		public IDatabase Database
		{
			get 
			{ 
				return _db; 
			}
			set 
			{ 
				_db = value; 
			}
		}
	}
}
