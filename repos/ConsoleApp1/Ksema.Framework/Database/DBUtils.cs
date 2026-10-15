using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using NPOI.SS.Formula.Functions;

namespace Ksema.Framework.Database
{
	public static class IDbCommandExtension
	{
		public static void AddParameter(this IDbCommand command, string name, object value)
		{
			IDbDataParameter par = command.CreateParameter();
			par.ParameterName = name;
			par.Value = value;
			command.Parameters.Add(par);
		}

		public static void AddParameter(this IDbCommand command, string name, object value, DbType dbType)
		{
			IDbDataParameter par = command.CreateParameter();
			par.ParameterName = name;
			par.Value = value;
			par.DbType = dbType;
			command.Parameters.Add(par);
		}
		public static void AddParameter(this IDbCommand command, string name, DbType dbType, ParameterDirection direction)
		{
			IDbDataParameter par = command.CreateParameter();
			par.ParameterName = name;
			par.DbType = dbType;
			par.Direction = direction;
			command.Parameters.Add(par);
		}
		public static void AddParameter(this IDbCommand command, string name, DbType dbType, ParameterDirection direction, object value, int defSize)
		{
			// Прототип ->  cmd_tb.Parameters.Add(new SqlParameter("@txt", SqlDbType.VarChar, 5000))
			IDbDataParameter par = command.CreateParameter();
			par.ParameterName = name;
			par.DbType = dbType;
			par.Value = value;
			par.Direction = direction;
			par.Size = defSize;
			command.Parameters.Add(par);
		}
		public static void AddParameter(this IDbCommand command, string name, DbType dbType, ParameterDirection direction, int defSize)
		{
			IDbDataParameter par = command.CreateParameter();
			par.ParameterName = name;
			par.DbType = dbType;
			par.Direction = direction;
			par.Size = defSize;
			command.Parameters.Add(par);
		}

	}
    public class DBCommonUtils
    {
		/// <summary>
		/// Для коллекции дает строчку, которую можно подставить в sql-запросы в конструкцию IN
		/// </summary>
		public static string CreateXmlParam<T>(IDbCommand command, string paramName, string dbNativeType, IEnumerable<T> valueList)
		{
			string xmlVal = BuildItemsXml(valueList);

			command.AddParameter(paramName, xmlVal, DbType.Xml);

			return string.Format("SELECT ParamValues.Item.value('.','{0}') FROM {1}.nodes('/ROOT/Item') as ParamValues(Item)", dbNativeType, paramName);

		}

	    /// <summary>
	    /// По списку значений создает xml-строчку для подстановки в запросы
	    /// </summary>
		public static string BuildItemsXml<K,V>(IEnumerable< KeyValuePair<K,V> > valueList, string key , string value)
		{
			string c_PrmTeg = "<{0}>{1}</{0}>";
			StringBuilder sb = new StringBuilder("<ROOT>");
			if (valueList != null)
				foreach (KeyValuePair<K,V> item in valueList)
				{
					var param1 = string.Format( c_PrmTeg, key , (string)HttpUtility.HtmlEncode(item.Key.ToString()) );
					var param2 = string.Format( c_PrmTeg, value , (string)HttpUtility.HtmlEncode(item.Value.ToString()) );
					sb.Append("<Item>").Append(param1+param2).Append("</Item>");
				}
			sb.Append("</ROOT>");
			return sb.ToString();
		}
		public static string BuildItemsXml<T>(IEnumerable<T> valueList)
		{
			StringBuilder sb = new StringBuilder("<ROOT>");
			if ( valueList != null )
				foreach (T item in valueList)
				{
					var param = HttpUtility.HtmlEncode(item.ToString());
					sb.Append("<Item>").Append(param).Append("</Item>");
				}
			sb.Append("</ROOT>");
			return sb.ToString();
		}
		public static string BuildItemsXmlSimple<T>(IEnumerable<T> valueList)
		{
			StringBuilder sb = new StringBuilder("<ROOT>");
			if (valueList != null)
				foreach (T item in valueList)
				{
					sb.Append("<Item>").Append(item.ToString()).Append("</Item>");
				}
			sb.Append("</ROOT>");
			return sb.ToString();
		}

        public static List<Tuple<string, string>> ReadItemsXmlSimple(string xml)
        {
            XDocument xmlDoc = XDocument.Parse(xml);
            List<Tuple<string, string>> items = xmlDoc.Descendants("Item")
                .Select(i => new Tuple<string, string>(i.Value.ElementAtOrDefault(0).ToString(),
                    i.Value.ElementAtOrDefault(1).ToString()))
                .ToList();

            return items;
        }

        private static void AddString( List<string> result, StringBuilder script )
        {
            string line = script.ToString();
            if( !String.IsNullOrEmpty( line ) && line.Trim() != String.Empty )
                result.Add( line );
        }

        /// <summary>
        /// Разбивает скрипт, содержащий инструкции GO, на скрипты.
        /// </summary>
        /// <param name="scriptText"></param>
        /// <returns></returns>
        public static IList<string> SplitScript( string scriptText )
        {
            return SplitScript( scriptText, "GO" );
        }

        /// <summary>
        /// Разбивает скрипт, содержащий инструкции GO, на скрипты.
        /// </summary>
        /// <param name="scriptText"></param>
        /// <param name="goStr"></param>
        /// <returns></returns>
        public static IList<string> SplitScript( string scriptText, string goStr )
        {
            ArrayList lines = new ArrayList();
            int start = 0;
            int stop = scriptText.IndexOf( Environment.NewLine );
            while( stop != -1 )
            {
                lines.Add( scriptText.Substring( start, stop - start ) );
                start = stop + Environment.NewLine.Length;
                stop = scriptText.IndexOf( Environment.NewLine, start );
            }
            lines.Add( scriptText.Substring( start ) );

            List<string> result = new List<string>();
            StringBuilder newScript = new StringBuilder();
            for( int i = 0; i < lines.Count; i++ )
            {
                string line = (string)lines[i];
                if( line != null
                    && String.Compare( line.Trim(), goStr, true ) == 0 )
                {
                    AddString( result, newScript );
                    newScript = new StringBuilder();
                }
                else
                    newScript.Append( line ).Append( Environment.NewLine );
            }
            if( newScript.Length > 0 )
                AddString( result, newScript );
            return result;
        }
    }
}
