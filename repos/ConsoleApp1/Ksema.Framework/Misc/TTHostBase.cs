using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TextTemplating;
using System.Data.Common;

namespace Shart.Service.Shared.Templates
{
	public class TTHostBase : ITextTemplatingEngineHost
	{
		readonly Dictionary<string, object> _arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		[XmlIgnore]
		public CompilerErrorCollection Errors { get; private set; }
		public string Method { get; set; }

		public void SetValue(string name, object value)
		{
			_arguments[name] = value;
		}

		public T GetValue<T>(string name)
		{
			return (T)_arguments[name];
		}

		public virtual IList<string> StandardAssemblyReferences
		{
			get 
			{ 
				string thisAssemblyPath = GetType().Assembly.Location;
				string folder = Path.GetDirectoryName(thisAssemblyPath);
				return new[]
				       	{
				       		typeof(CodeCompiler).Assembly.Location,
				       		typeof(DbParameter).Assembly.Location,
				       		thisAssemblyPath
				       	};
			}
		}

		public IList<string> StandardImports
		{
			get 
			{
				return new[]
				       	{
				       		"System"
				       	};
			}
		}

		public string TemplateFile
		{
			get { return String.Empty; }
		}

		public bool LoadIncludeText(string requestFileName, out string content, out string location)
		{
			content = String.Empty;
			location = String.Empty;

			if (File.Exists(requestFileName))
			{
				content = File.ReadAllText(requestFileName);
				return true;
			}
			return false;
		}

		public string ResolveAssemblyReference(string assemblyReference)
		{
			if (File.Exists(assemblyReference))
			{
				return assemblyReference;
			}

			return String.Empty;
		}

		public Type ResolveDirectiveProcessor(string processorName)
		{
			throw new NotImplementedException();
		}

		public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
		{
			throw new NotImplementedException();
		}

		public AppDomain ProvideTemplatingAppDomain(string content)
		{
			return AppDomain.CurrentDomain;
		}

		public void LogErrors(CompilerErrorCollection errors)
		{
			Errors = errors;
		}

		public void SetFileExtension(string extension)
		{
		}

		public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
		{
		}

		public object GetHostOption(string optionName)
		{
			object returnObject;
			switch (optionName)
			{
				case "CacheAssemblies":
					returnObject = true;
					break;
				default:
					returnObject = null;
					break;
			}
			return returnObject;
		}

		public string ResolvePath(string path)
		{
			return path;
		}

		public void ThrowExceptionIfNecessary()
		{
			if (Errors != null && Errors.HasErrors)
			{
				StringBuilder sb = new StringBuilder();
				foreach (CompilerError err in Errors)
				{
					sb.AppendLine(err.ToString());
				}
				throw new Exception(sb.ToString());
			}
		}

		public void ClearData()
		{
			_arguments.Clear();
		}
	}
}