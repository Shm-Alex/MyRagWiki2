using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Web;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Framework.Hyperlinks
{
    public class HyperLinkHelper
    {
        public static string ReadText(string hyperlinkText)
        {
            var sb = new StringBuilder();

            hyperlinkText = "<html>" + hyperlinkText + "</html>";

            Sgml.SgmlReader rdr = new Sgml.SgmlReader();
            rdr.DocType = "HTML";
            rdr.WhitespaceHandling = WhitespaceHandling.All;
            rdr.CaseFolding = Sgml.CaseFolding.ToLower;
            rdr.InputStream = new StringReader(hyperlinkText);

            rdr.Read();
            while (!rdr.EOF)
            {
                if (rdr.IsStartElement() && rdr.Name == "a")
                {
                    if (rdr.HasAttributes)
                    {
                        while (rdr.MoveToNextAttribute())
                        {
                        }
                    }
                    rdr.ReadStartElement();

                    var content = rdr.ReadContentAsString();
                    content = HttpUtility.HtmlDecode(content);
                    rdr.ReadEndElement();
                    sb.Append(content);
                }
                else if (rdr.NodeType == XmlNodeType.Text)
                {
                    sb.Append(HttpUtility.HtmlDecode(rdr.Value));
                    rdr.Read();
                }
                else
                {
                    rdr.Read();
                }
            }

            return sb.ToString();
        }

		public static IEnumerable<Inline> Convert(string hyperlinkText)
		{
			hyperlinkText = "<html>" + hyperlinkText + "</html>";

			Sgml.SgmlReader rdr = new Sgml.SgmlReader();
			rdr.DocType = "HTML";
			rdr.WhitespaceHandling = WhitespaceHandling.All;
			rdr.CaseFolding = Sgml.CaseFolding.ToLower;
			rdr.InputStream = new StringReader(hyperlinkText);

			rdr.Read();
			while (!rdr.EOF)
			{
				if (rdr.IsStartElement() && rdr.Name == "a")
				{
					var link = new Hyperlink();
					if (rdr.HasAttributes)
					{
						while (rdr.MoveToNextAttribute())
						{
							if (rdr.Name == "href")
							{
								try
								{
									var uriBild = new UriBuilder("shart", "service");
									uriBild.Path = HttpUtility.UrlDecode(rdr.Value);
									link.NavigateUri = uriBild.Uri;
								}
								catch
								{
									link.NavigateUri = null;
								}
							}
						}
					}
					rdr.ReadStartElement();

					var content = rdr.ReadContentAsString();
					content = HttpUtility.HtmlDecode(content);
					link.ToolTip = content;
					rdr.ReadEndElement();
					link.Inlines.Add(new Run(content));
					yield return link;
				}
				else if (rdr.NodeType == System.Xml.XmlNodeType.Text)
				{
					var run = new Run(HttpUtility.HtmlDecode(rdr.Value));
					rdr.Read();
					yield return run;
				}
				else
				{
					rdr.Read();
				}
			}
		}

		public static string GetLinkString(string text, string link)
		{
			const string format = "<a href=\"{0}\">{1}</a>";
			return String.Format(format, HttpUtility.UrlEncode(link), HttpUtility.HtmlEncode(text));
		}
	}
}
