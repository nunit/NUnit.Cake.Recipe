﻿/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER INTERFACES
/////////////////////////////////////////////////////////////////////////////

/// <Summary>
/// A runner capable of running unit tests
/// </Summary>
public interface IUnitTestRunner
{
    string PackageId { get; }
    string Version { get; }
    
	int RunUnitTest(FilePath testPath);
}

/// <Summary>
/// A runner capable of running package tests
/// </Summary>
public interface IPackageTestRunner
{
    string PackageId { get; }
    string Version { get; }

    IEnumerable<string> Output { get; }

    int RunPackageTest(string arguments, bool redirectOutput = false);
}

/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER BASE CLASS
/////////////////////////////////////////////////////////////////////////////

/// <summary>
/// The TestRunner class is the abstract base for all TestRunners used to run unit-
/// or package-tests. A TestRunner knows how to run both types of tests. The reason
/// for this design is that some derived runners are used for unit tests, others for
/// package tests and still others for both types. Derived classes implement one or
/// both interfaces to indicate what they support.
/// </summary>
public abstract class TestRunner
{
	protected ICakeContext Context => BuildSettings.Context;

	public string PackageId { get; protected set; }
	public string Version { get; protected set; }

	public IEnumerable<string> Output { get; protected set; }

    protected int RunUnitTest(FilePath executablePath, ProcessSettings processSettings)
    {
        if (executablePath == null)
            throw new ArgumentNullException(nameof(executablePath));

        if (processSettings == null)
            throw new ArgumentNullException(nameof(processSettings));

        // Add default values to settings if not present
        if (processSettings.WorkingDirectory == null)
            processSettings.WorkingDirectory = BuildSettings.OutputDirectory;

        return Context.StartProcess(executablePath, processSettings);
    }

	protected int RunPackageTest(FilePath executablePath, ProcessSettings processSettings)
	{
		if (executablePath == null)
			throw new ArgumentNullException(nameof(executablePath));

        if (processSettings == null)
            throw new ArgumentNullException(nameof(processSettings));
        
		// Add default values to settings if not present
        if (processSettings.WorkingDirectory == null)
			processSettings.WorkingDirectory = BuildSettings.OutputDirectory;

		// Was Output Requested?
		if (processSettings.RedirectStandardOutput)
			processSettings.RedirectedStandardOutputHandler = OutputHandler;

		IEnumerable<string> output;
		// If Redirected Output was not requested, output will be null
		int rc = Context.StartProcess(executablePath, processSettings, out output);
		Output = output;
		return rc;
    }

	internal string OutputHandler(string output)
	{
		// Ensure that package test output displays and is also re-directed.
		// If the derive class doesn't need the output, it doesn't retrieve it.
		Console.WriteLine(output);
		return output;
	}
}

/// <Summary>
/// A TestRunner requiring some sort of installation before use.
/// </Summary>
public abstract class InstallableTestRunner : TestRunner
{
	protected InstallableTestRunner(string packageId, string version)
	{
		PackageId = packageId;
		Version = version;
	}

	//private bool IsChocolateyPackage => PackageId.Contains('-'); // Hack!

	protected FilePath ExecutableRelativePath { get; set; }
	protected bool IsDotNetTool { get; set; } = false;

	// Path under tools directory where package would be installed by Cake #tool directive.
	// NOTE: When used to run unit tests, a #tool directive is required. If derived package
	// is only used for package tests, it is optional.
	protected DirectoryPath ToolInstallDirectory => IsDotNetTool
		? BuildSettings.ToolsDirectory
		: BuildSettings.ToolsDirectory + $"{PackageId}.{Version}";
	protected bool IsInstalledAsTool =>
		Context.DirectoryExists(ToolInstallDirectory);
	
	protected DirectoryPath InstallDirectory;

	public FilePath ExecutablePath => InstallDirectory.CombineWithFilePath(ExecutableRelativePath);

	public void Install(DirectoryPath installDirectory)
	{
		Context.Information($"Installing runner {PackageId} {Version} to directory {installDirectory}");
		InstallDirectory = installDirectory.Combine($"{PackageId}.{Version}");
		Context.CreateDirectory(InstallDirectory);

		// If the runner package is already installed as a cake tool, we just copy it
		if (IsInstalledAsTool)
			if (IsDotNetTool)
			{
				Context.CopyFileToDirectory(BuildSettings.ToolsDirectory + ExecutableRelativePath, InstallDirectory);
				Context.CopyDirectory(BuildSettings.ToolsDirectory + ".store", InstallDirectory);
			}
			else
				Context.CopyDirectory(ToolInstallDirectory, InstallDirectory);
		// Otherwise, we install it to the requested location
        else
            Context.NuGetInstall(
                PackageId,
                new NuGetInstallSettings() { OutputDirectory = installDirectory, Version = Version });
    }
}

/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER SOURCE
/////////////////////////////////////////////////////////////////////////////

/// <Summary>
/// TestRunnerSource is a provider of TestRunners. It is used when the tests
/// are to be run under multiple TestRunners rather than just one.
/// </Summary>
public class TestRunnerSource
{
	public TestRunnerSource(IPackageTestRunner runner1, params IPackageTestRunner[] moreRunners)
	{
		Runners.Add(runner1);
		Runners.AddRange(moreRunners);
	}

	public List<IPackageTestRunner> Runners = new List<IPackageTestRunner>();
}

/////////////////////////////////////////////////////////////////////////////
// NUNITLITE RUNNER
/////////////////////////////////////////////////////////////////////////////

// For NUnitLite tests, the test is run directly
public class NUnitLiteRunner : TestRunner, IUnitTestRunner
{
    public int RunUnitTest(FilePath testPath)
    {
        var processSettings = new ProcessSettings { Arguments = BuildSettings.UnitTestArguments };
        if (CommandLineOptions.TraceLevel.Exists)
            processSettings.EnvironmentVariables = new Dictionary<string, string>
            {
                { "NUNIT_INTERNAL_TRACE_LEVEL", CommandLineOptions.TraceLevel.Value }
            };

        return base.RunUnitTest(testPath, processSettings);
    }
}

/////////////////////////////////////////////////////////////////////////////
// NUNIT CONSOLE RUNNERS
/////////////////////////////////////////////////////////////////////////////

// NUnitConsoleRunner is used for both unit and package tests. It is normally
// pre-installed in the tools directory by use of a #tools directive.

// Abstract base for runners based on any of the NUnit Console Runners
public abstract class NUnitConsoleRunnerBase : InstallableTestRunner, IUnitTestRunner, IPackageTestRunner
{
    public NUnitConsoleRunnerBase(string packageId, string version) : base(packageId, version) { }

    // Run a unit test
    public int RunUnitTest(FilePath testPath) =>
        base.RunUnitTest(
            ToolInstallDirectory.CombineWithFilePath(ExecutableRelativePath),
            new ProcessSettings { Arguments = $"\"{testPath}\" {BuildSettings.UnitTestArguments}" });

    // Run a package test
    public int RunPackageTest(string arguments, bool redirectStandardOutput = false) =>
        base.RunPackageTest(ExecutablePath, new ProcessSettings { Arguments = arguments, RedirectStandardOutput = redirectStandardOutput });
}

// The standard v3/v4 console runner, running under .NET Framework and using agents
public class NUnitConsoleRunner : NUnitConsoleRunnerBase
{
    public NUnitConsoleRunner(string packageId, string version) : base(packageId, version)
    {
        ExecutableRelativePath = version[0] == '3' ? "tools/nunit3-console.exe" : "tools/nunit-console.exe";
    }
}

// The v3 netcore console runner
public class NUnit3NetCoreConsoleRunner : NUnitConsoleRunnerBase
{
    public NUnit3NetCoreConsoleRunner(string packageId, string version, string executable)
		: base(packageId, version)
    {
        ExecutableRelativePath = executable;
    }
}

// The v4 console runner
public class NUnit4DotNetRunner : NUnitConsoleRunnerBase
{
    public NUnit4DotNetRunner(string packageId, string version) : base(packageId, version)
    {
        IsDotNetTool = true;
        ExecutableRelativePath = "nunit.exe";
    }
}

//public class EngineExtensionTestRunner : TestRunner, IPackageTestRunner
//{
//	private IPackageTestRunner[] _runners = new IPackageTestRunner[] {
//		new NUnitConsoleRunner("3.17.0"),
//		new NUnitConsoleRunner("3.15.5")
//	};

//	public int RunPackageTest(string arguments)
//	{

//		return _runners[0].RunPackageTest(arguments);
//	}

//    public int RunPackageTest(string arguments, out string output)
//    {
//		var settings = new ProcessSettings
//		{
//			Arguments = arguments,
//			RedirectStandardOutput = true
//		};

//        return RunTest(ExecutablePath, settings, out output);
//    }
//}

/////////////////////////////////////////////////////////////////////////////
// AGENT RUNNER
/////////////////////////////////////////////////////////////////////////////

/// <summary>
/// Class that knows how to run an agent directly. (For future use)
/// </summary>
public class AgentRunner : TestRunner, IPackageTestRunner
{
    private string _stdExecutable;
    private string _x86Executable;

    private FilePath _executablePath;

	public AgentRunner(string stdExecutable, string x86Executable = null)
	{
        _stdExecutable = stdExecutable;
        _x86Executable = x86Executable;
    }

    public int RunPackageTest(string arguments, bool redirectOutput = false)
    {
        _executablePath = arguments.Contains("--x86")
            ? _x86Executable
            : _stdExecutable;

        return base.RunPackageTest(
			_executablePath, 
			new ProcessSettings { Arguments = arguments.Replace("--x86", string.Empty), RedirectStandardOutput = redirectOutput });
    }
}
