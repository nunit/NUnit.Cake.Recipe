// We use some recipe files for testing. In addition, loading the
// entire recipe gives us an error if any references are missing.
#load recipe/*.cake

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "NUnit Cake Recipe",
	githubRepository: "NUnit.Cake.Recipe");

//////////////////////////////////////////////////////////////////////
// DEFINE RECIPE PACKAGE
//////////////////////////////////////////////////////////////////////

BuildSettings.Packages.Add(
	new RecipePackage
	(
		id: "NUnit.Cake.Recipe",
		source: "NUnit.Cake.Recipe.nuspec",
		checks: new PackageCheck[] {
			HasFiles("README.md", "LICENSE.txt", "nunit_256.png"),
			HasDirectory("content").WithFiles(BuildSettings.Context.GetFiles("recipe/content/*").Select(f => f.GetFilename()).ToArray())
		}
	) );

//////////////////////////////////////////////////////////////////////
// TEST THE RECIPE
//////////////////////////////////////////////////////////////////////

// Keep the standard dependencies and criteria, but replace compiled-test discovery.
ClearActions(BuildTasks.UnitTestTask);
BuildTasks.UnitTestTask.Does(() =>
{
	// The harness changes static recipe settings, so run it in a separate process.
	var exitCode = StartProcess("dotnet", new ProcessSettings
	{
		Arguments = "tool run dotnet-cake -- harness/tests.cake --target=Harness --packageVersion=1.0.0 --nopush",
		WorkingDirectory = BuildSettings.ProjectDirectory
	});

	if (exitCode != 0)
		throw new CakeException($"Recipe harness failed with exit code {exitCode}. See harness/artifacts for available reports.");
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
