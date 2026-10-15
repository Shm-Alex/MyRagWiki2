using System;
using System.Xml;

namespace Serializer
{
	public class SerializerUtils
	{
		public static void InitIdentation(MetaData metaData, XmlTextWriter writer)
		{
			if (metaData.PrettyPrint)
			{
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
			}
		}

	}
}
