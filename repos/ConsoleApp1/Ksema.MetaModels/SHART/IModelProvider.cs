using System;
using System.Collections.Generic;
using Shart.ModelDef;
using Ksema.Framework.Database;

namespace Shart.Service.Model
{
	/// <summary>
	/// Операции по хранению моделей (в файлах, базе и т.п.) - получить, сохранить...
	/// </summary>
	public interface IModelProvider
	{
		void SaveModel( ShartModelMetaData model );
		IShartModelMetaData LoadModel( string name );

		DateTime GetModelChangeDate(IEnumerable<string> models);

		IEnumerable<string> GetModelsList();
		IDatabase Database
		{
			get;
			set;
		}
	}
}