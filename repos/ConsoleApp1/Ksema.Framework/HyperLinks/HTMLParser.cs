using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Sgml ;

namespace Ksema.Framework.HyperLinks
{
	public class HTMLParser
	{
		Sgml.SgmlReader _rdr = new Sgml.SgmlReader();
		public HTMLParser ( Stream stream )
		{
			_rdr.DocType = "html";
			_rdr.WhitespaceHandling = WhitespaceHandling.All;
			_rdr.CaseFolding = Sgml.CaseFolding.ToLower;
			_rdr.InputStream = new StreamReader(stream);
		}
		public int CloseTagPos (string tag)
		{
			for (;!_rdr.EOF;_rdr.Read())
			{
				if ( _rdr.Name != null && _rdr.Name.ToLower() == tag && !_rdr.IsStartElement() )
				{
					break;
				}
			}

			  int ofset = 0;

			while (_rdr.ReadChar())
				ofset++;
			
			return ofset ;
		}
		public void KillHTMLAfterTag( Stream stream , string tag )
		{
			int offest = CloseTagPos(tag);
			if ( offest > 0 )
				stream.SetLength(stream.Length - offest );
		}
	}

}
