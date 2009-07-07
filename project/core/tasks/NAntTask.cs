using System.Collections;
using System.IO;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core.Util;

namespace ThoughtWorks.CruiseControl.Core.Tasks
{
	[ReflectorType("nant")]
	public class NAntTask
        : BaseExecutableTask
	{
		public const int DefaultBuildTimeout = 600;
		public const string logFilename = "nant-results.xml";
		public const string defaultExecutable = "nant";
		public const string DefaultLogger = "NAnt.Core.XmlLogger";
		public const string DefaultListener = "NAnt.Core.DefaultLogger";
		public const bool DefaultNoLogo = true;

		public NAntTask(): 
			this(new ProcessExecutor()){}

		public NAntTask(ProcessExecutor executor)
		{
			this.executor = executor;
		}

		[ReflectorArray("targetList", Required = false)]
		public string[] Targets = new string[0];

		[ReflectorProperty("executable", Required = false)]
		public string Executable = defaultExecutable;

		[ReflectorProperty("buildFile", Required = false)]
		public string BuildFile = string.Empty;

		[ReflectorProperty("baseDirectory", Required = false)]
		public string ConfiguredBaseDirectory = string.Empty;

		[ReflectorProperty("buildArgs", Required = false)]
		public string BuildArgs = string.Empty;

		[ReflectorProperty("logger", Required = false)]
		public string Logger = DefaultLogger;

		[ReflectorProperty("listener", Required = false)]
		public string Listener = DefaultListener;

		[ReflectorProperty("nologo", Required = false)]
		public bool NoLogo = DefaultNoLogo;

		/// <summary>
		/// Gets and sets the maximum number of seconds that the build may take.  If the build process takes longer than
		/// this period, it will be killed.  Specify this value as zero to disable process timeouts.
		/// </summary>
		[ReflectorProperty("buildTimeoutSeconds", Required = false)]
		public int BuildTimeoutSeconds = DefaultBuildTimeout;

		/// <summary>
		/// Runs the integration using NAnt.  The build number is provided for labelling, build
		/// timeouts are enforced.  The specified targets are used for the specified NAnt build file.
		/// StdOut from nant.exe is redirected and stored.
		/// </summary>
		/// <param name="result">For storing build output.</param>
        protected override bool Execute(IIntegrationResult result)
		{
            result.BuildProgressInformation.SignalStartRunTask(!string.IsNullOrEmpty(Description) ? Description : 
                string.Format("Executing Nant :BuildFile: {0} Targets: {1} ", BuildFile, string.Join(", ", Targets)));

			ProcessResult processResult = TryToRun(CreateProcessInfo(result), result);

			string nantOutputFile = GetNantOutputFile(result);
			if (File.Exists(nantOutputFile))
				result.AddTaskResult(new FileTaskResult(nantOutputFile));

			result.AddTaskResult(new ProcessTaskResult(processResult));
			// is this right?? or should this break the build
			if (processResult.TimedOut)
				throw new BuilderException(this, "NAnt process timed out (after " + BuildTimeoutSeconds + " seconds)");

            return !processResult.Failed;
		}

		protected override string GetProcessFilename()
		{
			return Executable;
		}

		protected override int GetProcessTimeout()
		{
			return BuildTimeoutSeconds * 1000;
		}

		protected override string GetProcessArguments(IIntegrationResult result)
		{
			ProcessArgumentBuilder buffer = new ProcessArgumentBuilder();
			buffer.AppendIf(NoLogo, "-nologo");
			buffer.AppendArgument(@"-buildfile:{0}", StringUtil.AutoDoubleQuoteString(BuildFile));
			buffer.AppendArgument("-logger:{0}", Logger);
			buffer.AppendArgument("-logfile:{0}", StringUtil.AutoDoubleQuoteString(GetNantOutputFile(result)));
			buffer.AppendArgument("-listener:{0}", Listener);
			buffer.AppendArgument(BuildArgs);
			AppendIntegrationResultProperties(buffer, result);
			AppendTargets(buffer);
			return buffer.ToString();
		}

		protected override string GetProcessBaseDirectory(IIntegrationResult result)
		{
			return result.BaseFromWorkingDirectory(ConfiguredBaseDirectory);
		}

		private static void AppendIntegrationResultProperties(ProcessArgumentBuilder buffer, IIntegrationResult result)
		{
			// We have to sort this alphabetically, else the unit tests
			// that expect args in a certain order are unpredictable
			IDictionary properties = result.IntegrationProperties;
			foreach (string key in properties.Keys)
			{
				object value = result.IntegrationProperties[key];
				if (value != null)
					buffer.AppendArgument(string.Format("-D:{0}={1}", key, StringUtil.AutoDoubleQuoteString(StringUtil.RemoveTrailingPathDelimeter(StringUtil.IntegrationPropertyToString(value)))));
			}
		}

		private void AppendTargets(ProcessArgumentBuilder buffer)
		{
			foreach(string t in Targets)
			{
				buffer.AppendArgument(t);
			}
		}

		public override string ToString()
		{
			string baseDirectory = ConfiguredBaseDirectory ??string.Empty;
			return string.Format(@" BaseDirectory: {0}, Targets: {1}, Executable: {2}, BuildFile: {3}", baseDirectory, string.Join(", ", Targets), Executable, BuildFile);
		}

		public string TargetsForPresentation
		{
			get
			{
				return StringUtil.ArrayToNewLineSeparatedString(Targets);
			}
			set
			{
				Targets = StringUtil.NewLineSeparatedStringToArray(value);
			}
		}

		private static string GetNantOutputFile(IIntegrationResult result)
		{
			return Path.Combine(result.ArtifactDirectory, logFilename);
		}
	}
}
