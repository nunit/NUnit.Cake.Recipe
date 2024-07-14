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
	new NuGetPackage
	(
		id: "NUnit.Cake.Recipe",
		source: "nuget/NUnit.Cake.Recipe.nuspec"
		//description: "Cake Recipe used for building TestCentric applications and extensions",
		//releaseNotes: new [] {"line1", "line2", "line3"},
		//tags: new [] { "testcentric", "cake", "recipe" }
	) );

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
