using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Ksema.Framework.Diagnostics
{
	public class ModifiedTextWriterTraceListener : TraceListener
	{
		// Fields
		protected string fileName;
		internal TextWriter writer;

		// Methods
		public ModifiedTextWriterTraceListener()
		{
		}

		public ModifiedTextWriterTraceListener(Stream stream)
			: this(stream, string.Empty)
		{
		}

		public ModifiedTextWriterTraceListener(TextWriter writer)
			: this(writer, string.Empty)
		{
		}

		public ModifiedTextWriterTraceListener(string fileName)
		{
			this.fileName = fileName;
		}

		public ModifiedTextWriterTraceListener(Stream stream, string name)
			: base(name)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			this.writer = new StreamWriter(stream);
		}

		public ModifiedTextWriterTraceListener(TextWriter writer, string name)
			: base(name)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			this.writer = writer;
		}

		public ModifiedTextWriterTraceListener(string fileName, string name)
			: base(name)
		{
			this.fileName = fileName;
		}

		public override void Close()
		{
			if (this.writer != null)
			{
				this.writer.Close();
			}
			this.writer = null;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.Close();
			}
		}

		internal bool EnsureWriter()
		{
			bool flag = true;
			if (this.writer == null)
			{
				flag = false;
				if (this.fileName == null)
				{
					return flag;
				}
				Encoding encodingWithFallback = GetEncodingWithFallback(new UTF8Encoding(false));
				string fullPath = Path.GetFullPath(this.fileName);
				string directoryName = Path.GetDirectoryName(fullPath);
				string fileName = Path.GetFileName(fullPath);
				for (int i = 0; i < 2; i++)
				{
					try
					{
						this.writer = new StreamWriter(fullPath, true, encodingWithFallback, 0x1000);
						flag = true;
						break;
					}
					catch (IOException)
					{
						fileName = Guid.NewGuid().ToString() + fileName;
						fullPath = Path.Combine(directoryName, fileName);
					}
					catch (UnauthorizedAccessException)
					{
						break;
					}
					catch (Exception)
					{
						break;
					}
				}
				if (!flag)
				{
					this.fileName = null;
				}
			}
			return flag;
		}

		public override void Flush()
		{
			if (this.EnsureWriter())
			{
				this.writer.Flush();
			}
		}

		private static Encoding GetEncodingWithFallback(Encoding encoding)
		{
			Encoding encoding2 = (Encoding)encoding.Clone();
			encoding2.EncoderFallback = EncoderFallback.ReplacementFallback;
			encoding2.DecoderFallback = DecoderFallback.ReplacementFallback;
			return encoding2;
		}

		public override void Write(string message)
		{
			if (this.EnsureWriter())
			{
				if (base.NeedIndent)
				{
					this.WriteIndent();
				}
				this.writer.Write(message);
			}
		}

		public override void WriteLine(string message)
		{
			if (this.EnsureWriter())
			{
				if (base.NeedIndent)
				{
					this.WriteIndent();
				}
				this.writer.WriteLine(message);
				base.NeedIndent = true;
			}
		}

		// Properties
		public TextWriter Writer
		{
			get
			{
				this.EnsureWriter();
				return this.writer;
			}
			set
			{
				this.writer = value;
			}
		}

		public string FileName
		{
			get { return fileName; }
		}
	}
}
