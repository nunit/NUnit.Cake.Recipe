// This file defines what each of the tasks in the recipe actually does.
// You should not change these definitions unless you intend to change
// the behavior of a task for all projects that use the recipe.
//
// To make a change for a single project, you should add code to your build.cake
// or another project-specific cake file. See extending.cake for examples.

BuildTasks.DefaultTask = Task("Default")
    .Description("Default task if none specified by user")
    .IsDependentOn("Build");

BuildTasks.DumpSettingsTask = Task("DumpSettings")
	.Description("Display BuildSettings properties")
	.Does(() => BuildSettings.DumpSettings());

BuildTasks.CheckHeadersTask = Task("CheckHeaders")
	.Description("Check source files for valid copyright headers")
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.WithCriteria(() => !BuildSettings.SuppressHeaderCheck)
	.Does(() => Headers.Check());

BuildTasks.CleanTask = Task("Clean")
	.Description("Clean output and package directories")
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.Does(() => 
	{
		foreach (var binDir in GetDirectories($"**/bin/{BuildSettings.Configuration}/"))
			CleanDirectory(binDir);
        
        CleanDirectory(BuildSettings.PackageDirectory);
        CleanDirectory(BuildSettings.ImageDirectory);
        CleanDirectory(BuildSettings.ExtensionsDirectory);
		
        DeleteFiles(BuildSettings.ProjectDirectory + "*.log");
	});

BuildTasks.CleanAllTask = Task("CleanAll")
	.Description("Clean everything!")
	.Does(() => 
	{
		foreach (var binDir in GetDirectories("**/bin/"))
			CleanDirectory(binDir);

        CleanDirectory(BuildSettings.PackageDirectory);
        CleanDirectory(BuildSettings.ImageDirectory);
        CleanDirectory(BuildSettings.ExtensionsDirectory);

		DeleteFiles(BuildSettings.ProjectDirectory + "*.log");

		foreach (var dir in GetDirectories("src/**/obj/"))
			DeleteDirectory(dir, new DeleteDirectorySettings() { Recursive = true });
	});

BuildTasks.RestoreTask = Task("Restore")
	.Description("Restore referenced packages")
	.WithCriteria(() => BuildSettings.SolutionFile != null)
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.Does(() =>	{
		NuGetRestore(BuildSettings.SolutionFile, new NuGetRestoreSettings() {
		    Source = new string[]	{ 
                "https://www.nuget.org/api/v2",
                "https://www.myget.org/F/nunit/api/v2" },
			Verbosity = BuildSettings.NuGetVerbosity
        });
	});

BuildTasks.BuildTask = Task("Build")
	.WithCriteria(() => BuildSettings.SolutionFile != null)
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("CheckHeaders")
	.Description("Build the solution")
	.Does(() =>	{
		if (BuildSettings.BuildWithMSBuild)
            MSBuild(BuildSettings.SolutionFile, BuildSettings.MSBuildSettings);
		else
			DotNetBuild(BuildSettings.SolutionFile, BuildSettings.DotNetBuildSettings);
    });

BuildTasks.UnitTestTask = Task("Test")
	.Description("Run unit tests")
	.IsDependentOn("Build")
	.Does(() => UnitTesting.RunAllTests());

BuildTasks.PackageTask = Task("Package")
	.IsDependentOn("Build")
	.Description("Build, Install, Verify and Test all packages")
	.Does(() => {
		foreach(var package in BuildSettings.SelectedPackages)
    		package.BuildVerifyAndTest();
	});

BuildTasks.BuildTestAndPackageTask = Task("BuildTestAndPackage")
	.Description("Do Build, Test and Package all in one run")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

BuildTasks.PublishTask = Task("Publish")
	.Description("Publish all packages for current branch")
	.IsDependentOn("Package")
	.Does(() => {
		if (BuildSettings.ShouldPublishRelease)
			PackageReleaseManager.Publish();
		else
            Information("Nothing to publish from this run.");
    });

BuildTasks.PublishSymbolsPackageTask = Task("PublishSymbolsPackage")
	.Description("\"Re-publish a specific symbols package to NuGet after a failure\"")
	.Does(() => PackageReleaseManager.PublishSymbolsPackage());

BuildTasks.CreateDraftReleaseTask = Task("CreateDraftRelease")
	.Description("Create a draft release on GitHub")
	.Does(() => PackageReleaseManager.CreateDraftRelease() );

BuildTasks.CreateProductionReleaseTask = Task("CreateProductionRelease")
	.Description("Create a production GitHub Release")
	.Does(() => PackageReleaseManager.CreateProductionRelease() );

BuildTasks.ContinuousIntegrationTask = Task("ContinuousIntegration")
	.Description("Perform continuous integration run")
	.IsDependentOn("DumpSettings")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("Publish")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");
