using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shart.ModelDef
{
	public static class ShartModelExt
	{
		/// <summary>
		/// Возвращает все ссылки всех измерений на данное измерение.
		/// </summary>
		public static IList<IReferenceMetaData> GetDimensionReferencesList(this IDimensionMetaData referencedDim)
		{
			var dictionary = referencedDim.GetDimensionReferences();
			return dictionary.SelectMany(item => item.Value).ToList();
		}

		/// <summary>
		/// Возвращает все ссылки всех измерений на данное измерение.
		/// </summary>
		public static IDictionary<string, ICollection<IReferenceMetaData>> GetDimensionReferences(this IDimensionMetaData referencedDim)
		{
			Dictionary<string, ICollection<IReferenceMetaData>> result = new Dictionary<string, ICollection<IReferenceMetaData>>();
			foreach (IDimensionMetaData olapObjMetaData in referencedDim.Model.GetAllDimensionsMetaData())
			{
				ICollection<IReferenceMetaData> refs = GetReferences(olapObjMetaData, referencedDim);
				if (refs.Count > 0)
					result.Add(olapObjMetaData.Name, refs);
			}
			return result;
		}

		/// <summary>
		/// Возвращает все ссылки всех кубов на данное измерение.
		/// </summary>
		public static IDictionary<string, ICollection<IReferenceMetaData>> GetCubeReferences(this IDimensionMetaData referencedDim)
		{
			Dictionary<string, ICollection<IReferenceMetaData>> result = new Dictionary<string, ICollection<IReferenceMetaData>>();
			foreach (ICubeMetaData olapObjMetaData in referencedDim.Model.GetAllCubesMetaData())
			{
				ICollection<IReferenceMetaData> refs = GetReferences(olapObjMetaData, referencedDim);
				if (refs.Count > 0)
					result.Add(olapObjMetaData.Name, refs);
			}
			return result;
		}

		/// <summary>
		/// Возвращает все ссылки с первого объекта на второй
		/// </summary>
		public static ICollection<IReferenceMetaData> GetReferences(IOLAPObjectMetaData olapObjMetaData, IDimensionMetaData referencedDim)
		{
			List<IReferenceMetaData> result = new List<IReferenceMetaData>();
			foreach (IReferenceMetaData data in olapObjMetaData.GetReferences())
			{
				if (ReferenceEquals(data.Target, referencedDim))
					result.Add(data);
			}
			return result;
		}

		/// <summary>
		/// Возвращает занчение доп. свойства или значение по умолчанию, если свойство не задано
		/// </summary>
		public static T GetValueOrDefault<T>(this IOLAPObjectMetaData olapObject, string propertyName, T defaultValue)
		{
			object value = olapObject.GetValue(propertyName);
			return (T)(value ?? defaultValue);
		}
	}
}
