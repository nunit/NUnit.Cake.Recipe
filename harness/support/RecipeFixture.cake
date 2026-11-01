// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Cake.Testing;
using Cake.Testing.Fixtures;
using Cake.Core.Tooling;

/// <summary>
/// Executes maintained recipe code using Cake's real context and globber, with
/// an in-memory filesystem and intercepted child processes.
/// </summary>
public sealed class RecipeFixture
{
	public const string DependencyId = "Harness.Dependency";
	public FakeEnvironment Environment { get; }
	public FakeFileSystem FileSystem { get; }
	public FakeLog Log { get; } = new FakeLog();
	public RecipeProcessRunner ProcessRunner { get; } = new RecipeProcessRunner();
	public DirectoryPath Packages { get; }
	public DirectoryPath TestDirectory { get; }

	/// <summary>Supplies NuGet list output without contacting any feed.</summary>
	public void Respond(params string[] output) => ProcessRunner.Process.SetStandardOutput(output);

	/// <summary>Creates an installed dependency and optional relative files in memory.</summary>
	public void Install(string version, params string[] files)
	{
		var directory = Packages.Combine(DependencyId + "." + version);
		FileSystem.CreateDirectory(directory);
		foreach (string file in files) FileSystem.CreateFile(directory.CombineWithFilePath(file));
	}

	/// <summary>Creates an isolated package tree and initializes recipe settings.</summary>
	/// <param name="host">The script context supplying command-line arguments and registry access.</param>
	/// <param name="platform">The platform whose Cake paths should be exercised.</param>
	public RecipeFixture(ICakeContext host, PlatformFamily platform)
	{
		Environment = new FakeEnvironment(platform, false);
		Environment.WorkingDirectory = platform == PlatformFamily.Windows ? "C:/harness" : "/harness";
		FileSystem = new FakeFileSystem(Environment);
		var globber = new Globber(FileSystem, Environment);
		var configuration = new FakeConfiguration();
		var tools = new ToolLocator(Environment, new ToolRepository(Environment),
			new ToolResolutionStrategy(FileSystem, Environment, globber, configuration, Log));
		FileSystem.CreateFile(Environment.WorkingDirectory.CombineWithFilePath("tools/nuget.exe"));
		FileSystem.CreateFile("/usr/bin/mono");
		Packages = Environment.WorkingDirectory.Combine("packages");
		TestDirectory = Packages.Combine("Harness.Subject.1.0.0");
		FileSystem.CreateDirectory(TestDirectory);
		var context = new CakeContext(FileSystem, Environment, globber, Log,
			host.Arguments, ProcessRunner, host.Registry, tools, new CakeDataService(), configuration);
		BuildSettings.Initialize(context, "Recipe Fixture", "NUnit.Cake.Recipe", solutionFile: "Harness.sln");
	}
}

/// <summary>Records requested tool invocations without starting an operating-system process.</summary>
public sealed class RecipeProcessRunner : IProcessRunner
{
	public FakeProcess Process { get; } = new FakeProcess();
	public List<ToolFixtureResult> Results { get; } = new List<ToolFixtureResult>();

	/// <inheritdoc />
	public IProcess Start(FilePath path, ProcessSettings settings)
	{
		if (!settings.RedirectStandardOutput || !settings.Arguments.Render().StartsWith("list "))
			throw new InvalidOperationException("Unexpected process request: " + path + " " + settings.Arguments.Render());
		Results.Add(new ToolFixtureResult(path, settings));
		return Process;
	}
}
