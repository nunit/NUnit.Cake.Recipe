// We use some recipe files for testing. In addition, loading the
// entire recipe gives us an error if any references are missing.
#load src/NUnit.Cake.Recipe/content/*.cake

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
	new NuGetPackage
	(
		id: "NUnit.Cake.Recipe",
		source: "src/NUnit.Cake.Recipe/NUnit.Cake.Recipe.csproj",
		checks: new PackageCheck[] {
			HasFiles("README.md", "LICENSE.txt", "nunit_256.png"),
			HasDirectory("content").WithFiles(BuildSettings.Context.GetFiles("recipe/content/*").Select(f => f.GetFilename()).ToArray())
		}
	) );

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
