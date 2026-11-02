// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#addin nuget:?package=Cake.Testing&version=6.3.0
#load ../recipe/*.cake
#load support/*.cake

// Keep this entry point separate from the recipe's build and publish targets.
var allowedTargets = new[] { "Harness", "Harness-Tests", "Harness-Smoke" };
if (CommandLineOptions.Targets.Values.Any(target => !allowedTargets.Contains(target)))
	throw new ArgumentException("Select --target=Harness, Harness-Tests or Harness-Smoke.");
if (CommandLineOptions.PackageVersion.Value != "1.0.0")
	throw new ArgumentException("Use --packageVersion=1.0.0 to avoid deriving a version from Git.");

BuildSettings.Initialize(Context, "Recipe Harness", "NUnit.Cake.Recipe", solutionFile: "Harness.sln");
BuildSettings.Packages.Add(new NuGetPackage("Harness.Unpublished", packageVersion: "1.0.0"));

Task("Harness-Tests").Does(() =>
{
	try { RunCases(); }
	finally
	{
		BuildSettings.Initialize(Context, "Recipe Harness", "NUnit.Cake.Recipe", solutionFile: "Harness.sln");
	}
});
Task("Harness-Smoke").Does(() =>
{
	var suite = new RecipeTestSuite(Context);
	suite.Add("real-cake/package-verification-and-failure", () => RunSmoke());
	suite.Run("artifacts/smoke.xml");
});
Task("Harness").IsDependentOn("Harness-Tests").IsDependentOn("Harness-Smoke");

Build.Run();

void RunCases()
{
	var suite = new RecipeTestSuite(Context);
	var selectors = new[]
	{
		new RecipeSelector("stable", check => check.LatestStableRelease,
			package => package.LatestStableRelease, "2.0.0", false, false),
		new RecipeSelector("prerelease", check => check.LatestReleaseIncludingPreReleases,
			package => package.LatestReleaseIncludingPreReleases, "2.0.0-beta.1", true, false),
		new RecipeSelector("development", check => check.LatestDevBuild,
			package => package.LatestDevBuild, "2.0.0-dev.1", true, true)
	};
	foreach (var platform in new[] { PlatformFamily.Windows, PlatformFamily.Linux })
	{
		foreach (var selector in selectors)
		{
			string prefix = platform + "/" + selector.Name + "/";
			suite.Add(prefix + "construction-is-deferred", () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				selector.Check(HasDependency(RecipeFixture.DependencyId));
				HarnessAssert.Equal(0, fixture.ProcessRunner.Results.Count, "lookup count");
			});
			suite.Add(prefix + "apply-resolves-exact-version-and-files", () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				fixture.Respond(RecipeFixture.DependencyId + " " + selector.Version);
				fixture.Install(selector.Version, "lib/dependency.dll");
				fixture.Install("1.0.0");
				var check = selector.Check(HasDependency(RecipeFixture.DependencyId))
					.WithDirectory("lib").WithFile("lib/dependency.dll");
				HarnessAssert.True(check.ApplyTo(fixture.TestDirectory), "resolved package checks");
				HarnessAssert.Equal(1, fixture.ProcessRunner.Results.Count, "lookup count");
				AssertLookup(fixture, selector);
			});
			suite.Add(prefix + "resolved-version-rejects-older-installation", () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				fixture.Respond(RecipeFixture.DependencyId + " " + selector.Version);
				fixture.Install("1.0.0");
				HarnessAssert.True(!selector.Check(HasDependency(RecipeFixture.DependencyId))
					.ApplyTo(fixture.TestDirectory), "older installed package must fail");
			});
			suite.Add(prefix + "lookup-selects-exact-id-and-source", () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				fixture.Respond(RecipeFixture.DependencyId + ".Extras 99.0.0",
					RecipeFixture.DependencyId + " " + selector.Version);
				var result = selector.Package(new PackageReference(RecipeFixture.DependencyId));
				HarnessAssert.Equal(RecipeFixture.DependencyId, result.Id, "package id");
				HarnessAssert.Equal(selector.Version, result.Version, "package version");
				AssertLookup(fixture, selector);
			});
			// #130: failed latest lookups must not become unversioned checks,
			// or silently retain an originally pinned PackageReference version.
			foreach (bool unrelated in new[] { false, true })
			{
				string response = unrelated ? "unrelated-result" : "empty-result";
				foreach (string originalVersion in new[] { (string)null, "1.0.0" })
				{
					string pin = originalVersion == null ? "unversioned" : "originally-pinned";
					suite.Add(prefix + response + "/" + pin + "/lookup-aborts", () =>
					{
						var fixture = new RecipeFixture(Context, platform);
						fixture.Respond(unrelated ? new[] { RecipeFixture.DependencyId + ".Extras 99.0.0" } : new string[0]);
						HarnessAssert.Throws<InvalidOperationException>(() =>
							selector.Package(new PackageReference(RecipeFixture.DependencyId, originalVersion)), RecipeFixture.DependencyId);
						HarnessAssert.Equal(1, fixture.ProcessRunner.Results.Count, "lookup count");
					});
				}
				foreach (int installedCount in new[] { 0, 1, 2 })
				{
					suite.Add(prefix + response + "/installed-" + installedCount + "/check-aborts", () =>
					{
						var fixture = new RecipeFixture(Context, platform);
						fixture.Respond(unrelated ? new[] { RecipeFixture.DependencyId + ".Extras 99.0.0" } : new string[0]);
						for (int i = 0; i < installedCount; i++) fixture.Install("1.0." + i);
						var check = selector.Check(HasDependency(RecipeFixture.DependencyId));
						HarnessAssert.Throws<InvalidOperationException>(() => check.ApplyTo(fixture.TestDirectory), RecipeFixture.DependencyId);
					});
				}
			}
			suite.Add(prefix + "tool-failure-rejects-installed-fallback", () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				fixture.Install("1.0.0");
				fixture.ProcessRunner.Process.SetExitCode(1);
				var check = selector.Check(HasDependency(RecipeFixture.DependencyId));
				HarnessAssert.Throws<CakeException>(() => check.ApplyTo(fixture.TestDirectory), "NuGet");
			});
		}
		suite.Add(platform + "/four-agent-checks-remain-deferred", () =>
		{
			var fixture = new RecipeFixture(Context, platform);
			foreach (string framework in new[] { "Net462", "Net80", "Net90", "Net100" })
			{
				var extension = new ExtensionSpecifier("NUnit.Extension." + framework + "PluggableAgent", null);
				// The deferred call-site form used in nunit-console #1944.
				HasDependency(extension.NuGetPackage).LatestStableRelease.WithDirectory("tools");
			}
			HarnessAssert.Equal(0, fixture.ProcessRunner.Results.Count, "lookup count");
		});
		suite.Add(platform + "/explicit-extension-selector-resolves", () =>
		{
			var fixture = new RecipeFixture(Context, platform);
			fixture.Respond(RecipeFixture.DependencyId + " 2.0.0");
			var extension = new ExtensionSpecifier(RecipeFixture.DependencyId, "harness-dependency", "1.0.0");
			var resolved = extension.LatestStableRelease;
			HarnessAssert.Equal("2.0.0", resolved.NuGetPackage.Version, "resolved NuGet version");
			HarnessAssert.Equal("harness-dependency", resolved.ChocoPackage.Id, "retained Chocolatey id");
			HarnessAssert.Equal("1.0.0", extension.Version, "original extension version");
			HarnessAssert.Equal(1, fixture.ProcessRunner.Results.Count, "lookup count");
		});
		foreach (int installedCount in new[] { 0, 1, 2 })
		{
			suite.Add(platform + "/unversioned/installed-" + installedCount, () =>
			{
				var fixture = new RecipeFixture(Context, platform);
				for (int i = 0; i < installedCount; i++) fixture.Install("1.0." + i);
				bool result = HasDependency(RecipeFixture.DependencyId).ApplyTo(fixture.TestDirectory);
				HarnessAssert.Equal(installedCount == 1, result, "unversioned check result");
				HarnessAssert.Equal(0, fixture.ProcessRunner.Results.Count, "lookup count");
			});
		}
		suite.Add(platform + "/verification-aborts-after-unresolved-latest", () =>
		{
			var fixture = new RecipeFixture(Context, platform);
			fixture.Respond();
			fixture.Install("1.0.0");
			var sentinel = new HarnessSentinelCheck();
			var package = new HarnessPackage(fixture.TestDirectory, new PackageCheck[]
			{
				HasDependency(RecipeFixture.DependencyId).LatestStableRelease, sentinel
			});
			HarnessAssert.Throws<InvalidOperationException>(() => package.VerifyPackage(), RecipeFixture.DependencyId);
			HarnessAssert.Equal(0, sentinel.Calls, "subsequent check count");
		});
	}
	suite.Run("artifacts/tests.xml");
}

void AssertLookup(RecipeFixture fixture, RecipeSelector selector)
{
	string args = fixture.ProcessRunner.Results.Single().Args;
	HarnessAssert.True(args.StartsWith("list "), "NuGet list command");
	HarnessAssert.True(args.Contains("\"" + RecipeFixture.DependencyId + "\""), "requested id");
	HarnessAssert.Equal(selector.Prerelease, args.Contains("-Prerelease"), "prerelease option");
	HarnessAssert.Equal(selector.Development, args.Contains("https://www.myget.org/F/nunit/api/v3/index.json"), "development source");
	HarnessAssert.Equal(!selector.Development, args.Contains("https://www.nuget.org/api/v2/"), "production NuGet source");
	HarnessAssert.Equal(!selector.Development, args.Contains("https://community.chocolatey.org/api/v2/"), "production Chocolatey source");
}

void RunSmoke()
{
	var root = MakeAbsolute(Directory("artifacts")).Combine("smoke-" + Guid.NewGuid().ToString("N"));
	var subject = root.Combine("Harness.Subject.1.0.0");
	var dependency = root.Combine(RecipeFixture.DependencyId + ".2.0.0");
	try
	{
		EnsureDirectoryExists(subject.Combine("lib"));
		EnsureDirectoryExists(dependency);
		System.IO.File.WriteAllText(subject.CombineWithFilePath("payload.txt").FullPath, "fixture");
		System.IO.File.WriteAllText(subject.CombineWithFilePath("lib/example.dll").FullPath, "fixture");
		System.IO.File.WriteAllText(dependency.CombineWithFilePath("dependency.txt").FullPath, "fixture");
		var package = new HarnessPackage(subject, new PackageCheck[]
		{
			HasFile("payload.txt"), HasDirectory("lib").WithFile("example.dll"),
			HasDependency(RecipeFixture.DependencyId, "2.0.0").WithFile("dependency.txt")
		});
		package.VerifyPackage();
		DeleteFile(subject.CombineWithFilePath("payload.txt"));

		// Capture only the deliberately failing verification. Keep the real
		// filesystem and assert its diagnostic instead of printing a false alarm.
		var expectedLog = new Cake.Testing.FakeLog();
		var failureContext = new CakeContext(Context.FileSystem, Context.Environment,
			Context.Globber, expectedLog, Context.Arguments, Context.ProcessRunner,
			Context.Registry, Context.Tools, new CakeDataService(), Context.Configuration);
		BuildSettings.Initialize(failureContext, "Recipe Harness", "NUnit.Cake.Recipe", solutionFile: "Harness.sln");
		try
		{
			var missingPayload = new HarnessPackage(subject, new PackageCheck[] { HasFile("payload.txt") });
			HarnessAssert.Throws<Exception>(() => missingPayload.VerifyPackage(), "Verification failed!");
			var errors = expectedLog.Entries.Where(entry => entry.Level == Cake.Core.Diagnostics.LogLevel.Error).ToArray();
			HarnessAssert.Equal(1, errors.Length, "expected missing-file diagnostic count");
			HarnessAssert.True(errors[0].Message.Contains(
				"File " + subject.CombineWithFilePath("payload.txt") + " was not found."), "expected missing-file diagnostic");
		}
		finally
		{
			BuildSettings.Initialize(Context, "Recipe Harness", "NUnit.Cake.Recipe", solutionFile: "Harness.sln");
		}
		Information("Expected missing-file failure verified (payload.txt).");
	}
	finally
	{
		if (DirectoryExists(root)) DeleteDirectory(root, new DeleteDirectorySettings { Recursive = true });
	}
	HarnessAssert.True(!DirectoryExists(root), "smoke fixture cleanup");
}
