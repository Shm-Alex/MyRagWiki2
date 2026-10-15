using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using Framework.Utils;
using System.Diagnostics;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using Microsoft.Win32;
using System.Configuration;
using System.Xml;
using System.Globalization;
using System.Text;

namespace Ksema.Framework
{
	public class SnippetCompilerController
	{
		private readonly CultureInfo _uiCulture;
		private Assembly _tempAssembly;
		private Thread _scriptThread;
		List<string> _compilerVersions = new List<string>();
		CompilerErrorCollection _buildErrors = new CompilerErrorCollection();
		Exception _runError;

		public SnippetCompilerController(CultureInfo uiCulture)
		{
			_uiCulture = uiCulture;
			LoadDefaultProject();
			LoadCompilerVersions();
		}

		public event EventHandler BeginRun;
		public event EventHandler EndRun;
		public event EventHandler BeginBuild;
		public event EventHandler BuildFailed;
		public event EventHandler BuildSucceeded;

		public void InvokeBeginRun()
		{
			if (BeginRun != null) BeginRun(this, EventArgs.Empty);
		}
		public void InvokeEndRun()
		{
			if (EndRun != null) EndRun(this, EventArgs.Empty);
		}
		public void InvokeBeginBuild()
		{
			if (BeginBuild != null) BeginBuild(this, EventArgs.Empty);
		}
		public void InvokeBuildFailed()
		{
			if (BuildFailed != null) BuildFailed(this, EventArgs.Empty);
		}
		public void InvokeBuildSucceeded()
		{
			if (BuildSucceeded != null) BuildSucceeded(this, EventArgs.Empty);
		}

		public CompilerErrorCollection BuildErrors
		{
			get { return _buildErrors; }
		}

		public Exception RunError
		{
			get
			{
				return _runError;
			}
		}

		public List<string> CompilerVersions
		{
			get { return _compilerVersions; }
		}

		public SnippetCompilerProject Project { get; set; }

		public void LoadProject(string fileName)
		{
			XmlSerializer ser = new XmlSerializer(typeof(SnippetCompilerProject));
			using (var reader = new XmlTextReader(fileName))
			{
				Project = (SnippetCompilerProject)ser.Deserialize(reader);
			}
		}

		public void SaveProject(string fileName)
		{
			XmlSerializer ser = new XmlSerializer(typeof(SnippetCompilerProject));
			using (var writer = new XmlTextWriter(fileName, Encoding.GetEncoding(1251)))
			{
				writer.Formatting = Formatting.Indented;
				ser.Serialize(writer, Project);
			}
		}

		public void LoadDefaultProject()
		{
			XmlSerializer ser = new XmlSerializer(typeof (SnippetCompilerProject));
			using (var stream = AppUtils.GetResourceStream("Ksema.Framework.SnippetCompiler.snippetcompiler.xml,Ksema.Framework"))
			{
				Project = (SnippetCompilerProject) ser.Deserialize(stream);
			}
			Project.CompilerVersion = DefaultCompilerVersion;
		}

		public bool CanStopRun()
		{
			return _scriptThread != null;
		}

		public bool CanRun()
		{
			return _scriptThread == null;
		}

		public void StopRun()
		{
			if (CanStopRun())
			{
				_scriptThread.Abort();
				_scriptThread = null;
				InvokeEndRun();
			}
		}

		public void Run()
		{
			if( CanRun() )
			{
				if (Build())
				{
					InvokeBeginRun();

					_runError = null;
					_scriptThread = new Thread(RunScript);
					_scriptThread.SetApartmentState(ApartmentState.STA);
					_scriptThread.CurrentUICulture = _uiCulture;
					_scriptThread.Start();
				}
			}
		}

		private void RunScript()
		{
			if( _tempAssembly != null )
			{
				try
				{
					CallEntry( _tempAssembly, "Main" );
				}
				catch (Exception error)
				{
					_runError = error;
				}
				finally
				{
					_scriptThread = null;
					InvokeEndRun();
				}
			}
		}

		public bool Build()
		{
			if (!CanRun())
				return false;

			bool result = true;
			_tempAssembly = null;
			try
			{
				InvokeBeginBuild();
				CompilerResults compilerResults;
				_tempAssembly = CreateAssembly( Project.Code, out compilerResults );
				if (compilerResults.Errors.HasErrors)
				{
					result = false;
					_buildErrors = compilerResults.Errors;
					InvokeBuildFailed();
				}
				else
				{
					_buildErrors = new CompilerErrorCollection();
					InvokeBuildSucceeded();
				}
			}
			catch( Exception exc )
			{
				result = false;
				_buildErrors = new CompilerErrorCollection();
				_buildErrors.Add(new CompilerError("", 0, 0, "", exc.Message));
				InvokeBuildFailed();
			}
			return result;
		}

		private Assembly CreateAssembly(string strRealSourceCode, out CompilerResults results)
		{
			Dictionary<string, string> opts = new Dictionary<string, string>();
			opts.Add("CompilerVersion", Project.CompilerVersion);

			CodeDomProvider codeProvider = new CSharpCodeProvider(opts);

			//add compiler parameters
			string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

			CompilerParameters compilerParams = new CompilerParameters();
			var path = AppDomain.CurrentDomain.BaseDirectory;
			//compilerParams.CompilerOptions = String.Format("/target:library /define:DEBUG;TRACE /lib:{0},{1}", Bracket(path), Bracket(progFiles));
			compilerParams.CompilerOptions = String.Format( "/target:library /define:DEBUG;TRACE /lib:{0}", Bracket( progFiles ) );
			// you can add /optimize

			compilerParams.GenerateExecutable = false;
			compilerParams.GenerateInMemory = true;
			compilerParams.IncludeDebugInformation = false;
			compilerParams.WarningLevel = 4;

			compilerParams.ReferencedAssemblies.AddRange(GetAssemblies(AppDomain.CurrentDomain.BaseDirectory, "*.dll"));
			compilerParams.ReferencedAssemblies.AddRange(GetAssemblies(AppDomain.CurrentDomain.BaseDirectory, "*.exe"));

			compilerParams.ReferencedAssemblies.AddRange(Project.ReferencedAssemblies);

			results = codeProvider.CompileAssemblyFromSource(compilerParams, strRealSourceCode);

			if (results.Errors.HasErrors)
				return null;

			return results.CompiledAssembly;
		}

		private string Bracket(string path)
		{
			return path.IndexOf(' ') > 0 ? ("\"" + path + "\"") : path;
		}

		private Dictionary<string, string[]> _assemliesListCache = new Dictionary<string, string[]>();

		[DebuggerNonUserCode]
		private string[] GetAssemblies(string path, string searchPattern)
		{
			string[] result;
			if (_assemliesListCache.ContainsKey(path))
			{
				result = _assemliesListCache[path];
			}
			else
			{
				List<string> assemblies = new List<string>();
				foreach (string assemblyFile in Directory.GetFiles(path, searchPattern))
				{
					try
					{
						Assembly.LoadFrom(assemblyFile);
						var fileName = Path.GetFileName(assemblyFile);
						if (fileName.IndexOf(".vshost.") < 0)
						{
							assemblies.Add(fileName);
						}
					}
					catch
					{
					}
				}
				result = assemblies.ToArray();
				_assemliesListCache.Add(path, result);
			}
			return result;
		}

		private static void CallEntry(Assembly assembly, string entryPoint)
		{
			Module[] mods = assembly.GetModules(false);
			Type[] types = mods[0].GetTypes();

			bool entryPointFound = false;
			foreach (Type type in types)
			{
				MethodInfo mi = type.GetMethod(entryPoint, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (mi != null)
				{
					if (mi.GetParameters().Length == 1)
					{
						if (mi.GetParameters()[0].ParameterType.IsArray)
						{
							entryPointFound = true;
							string[] par = new string[1]; // if Main has string [] arguments
							mi.Invoke(null, par);
						}
					}
					else
					{
						entryPointFound = true;
						mi.Invoke( null, null );
					}
					break;
				}
			}
			if( !entryPointFound )
			{
				throw new EntryPointNotFoundException( "Entry point '" +  entryPoint + "' not found");
			}
		}



		private void LoadCompilerVersions()
		{
			const string keyPath = @"Software\Microsoft\MSBuild\ToolsVersions\";
			RegistryKey key = Registry.LocalMachine.OpenSubKey( keyPath, false );

			List<string> verList = new List<string>( key.GetSubKeyNames() );
			verList.Sort();

			_compilerVersions.Clear();
			foreach( string ver in verList )
			{
				_compilerVersions.Add( "v" + ver );
			}
		}

		public static string DefaultCompilerVersion
		{
			get
			{
				return ConfigurationManager.AppSettings["snippetCompilerDefaultVersion"];
			}
		}

	}
}
