// We use some recipe files for testing. In addition, loading the
// entire recipe gives us an error if any references are missing.
#load recipe/*.cake

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "NUnit Cake Recipe",
	githubRepository: "NUnit.Cake.Recipe",
	defaultTarget: "Package" );

//////////////////////////////////////////////////////////////////////
// DEFINE RECIPE PACKAGE
//////////////////////////////////////////////////////////////////////

BuildSettings.Packages.Add(
	new RecipePackage
	(
		id: "NUnit.Cake.Recipe",
		source: "nuget/NUnit.Cake.Recipe.nuspec",
		checks: new PackageCheck[] {
			HasFiles("README.md", "LICENSE.txt", "nunit_256.png"),
			HasDirectory("content").WithFiles(BuildSettings.Context.GetFiles("recipe/*.cake").Select(f => f.GetFilename()).ToArray())
		}
	) );

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
