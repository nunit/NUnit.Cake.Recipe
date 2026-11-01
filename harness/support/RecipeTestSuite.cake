// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Diagnostics;
using System.Xml.Linq;

/// <summary>Runs every case and writes JUnit XML before failing the Cake target for any failures.</summary>
public sealed class RecipeTestSuite
{
	private readonly ICakeContext _host;
	private readonly List<(string Name, Action Body)> _cases = new List<(string, Action)>();

	/// <summary>Uses the real script context for output and report files.</summary>
	public RecipeTestSuite(ICakeContext host) => _host = host;

	/// <summary>Adds a named case whose body executes only when the suite runs.</summary>
	public void Add(string name, Action body) => _cases.Add((name, body));

	/// <summary>Runs all cases and saves the report before throwing for failures.</summary>
	public void Run(FilePath reportPath)
	{
		var results = new XElement("testsuite", new XAttribute("name", "Recipe source harness"));
		int failed = 0;
		foreach (var test in _cases)
		{
			var timer = Stopwatch.StartNew();
			var result = new XElement("testcase", new XAttribute("name", test.Name));
			try
			{
				test.Body();
				_host.Information("PASS {0}", test.Name);
			}
			catch (Exception ex)
			{
				failed++;
				result.Add(new XElement("failure", new XAttribute("message", ex.Message), ex.ToString()));
				_host.Error("FAIL {0}: {1}", test.Name, ex.Message);
			}
			result.Add(new XAttribute("time", timer.Elapsed.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
			results.Add(result);
		}
		results.Add(new XAttribute("tests", _cases.Count), new XAttribute("failures", failed), new XAttribute("errors", 0));
		var absolutePath = reportPath.MakeAbsolute(_host.Environment);
		_host.EnsureDirectoryExists(absolutePath.GetDirectory());
		new XDocument(new XElement("testsuites", results)).Save(absolutePath.FullPath);
		_host.Information("{0}/{1} cases passed. Report: {2}", _cases.Count - failed, _cases.Count, absolutePath);
		if (failed != 0) throw new CakeException(failed + " recipe harness cases failed.");
	}
}

/// <summary>Assertions that require no separate test-runner process.</summary>
public static class HarnessAssert
{
	/// <summary>Fails with a diagnostic when a condition is false.</summary>
	public static void True(bool condition, string message)
	{
		if (!condition) throw new Exception(message);
	}

	/// <summary>Compares observable values.</summary>
	public static void Equal<T>(T expected, T actual, string message)
		=> True(EqualityComparer<T>.Default.Equals(expected, actual), message + ": expected " + expected + ", actual " + actual);

	/// <summary>Requires an exception of the specified type with an identifying diagnostic.</summary>
	public static void Throws<T>(Action action, string messageFragment) where T : Exception
	{
		try { action(); }
		catch (T ex)
		{
			True(ex.Message.Contains(messageFragment), "Exception did not identify " + messageFragment + ": " + ex.Message);
			return;
		}
		throw new Exception("Expected " + typeof(T).Name + " identifying " + messageFragment + ".");
	}
}

/// <summary>Pairs the two public selector paths and their expected NuGet options.</summary>
public sealed class RecipeSelector
{
	public string Name { get; }
	public Func<DependencyCheck, DependencyCheck> Check { get; }
	public Func<PackageReference, PackageReference> Package { get; }
	public string Version { get; }
	public bool Prerelease { get; }
	public bool Development { get; }

	/// <summary>Describes a stable, prerelease or development selector.</summary>
	public RecipeSelector(string name, Func<DependencyCheck, DependencyCheck> check,
		Func<PackageReference, PackageReference> package, string version, bool prerelease, bool development)
	{
		Name = name;
		Check = check;
		Package = package;
		Version = version;
		Prerelease = prerelease;
		Development = development;
	}
}

/// <summary>Uses real package verification with a caller-owned installation directory.</summary>
public sealed class HarnessPackage : NuGetPackage
{
	private readonly DirectoryPath _directory;

	/// <summary>Creates an unpublished package fixture; it is never built or installed.</summary>
	public HarnessPackage(DirectoryPath directory, PackageCheck[] checks)
		: base("Harness.Subject", packageVersion: "1.0.0", checks: checks) => _directory = directory;

	/// <inheritdoc />
	public override string PackageTestDirectory => _directory.FullPath;
}

/// <summary>Detects whether verification continues after an unresolved lookup.</summary>
public sealed class HarnessSentinelCheck : PackageCheck
{
	public int Calls { get; private set; }

	/// <inheritdoc />
	public override bool ApplyTo(DirectoryPath directory)
	{
		Calls++;
		return true;
	}
}
