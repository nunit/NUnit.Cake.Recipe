// All tasks incorporated in the recipe are defined using CakeTaskBuilders.
// The actual specification of criteria, dependencies and actions for each
// task is done separately in task-definitions.cake.
//
// This approach provides a level of indirection, permitting the user to
// modify or completely redefine what a task does in their build.cake file,
// without changing the definitions in the recipe.

public static class BuildTasks
{
	// General
	public static CakeTaskBuilder DumpSettingsTask { get; set; }
	public static CakeTaskBuilder DefaultTask {get; set; }

	// Building
	public static CakeTaskBuilder BuildTask { get; set; }
	public static CakeTaskBuilder CheckHeadersTask { get; set; }
	public static CakeTaskBuilder CleanTask { get; set; }
	public static CakeTaskBuilder CleanAllTask { get; set; }
	public static CakeTaskBuilder RestoreTask { get; set; }

	// Unit Testing
	public static CakeTaskBuilder UnitTestTask { get; set; }

	// Packaging
	public static CakeTaskBuilder PackageTask { get; set; }
	public static CakeTaskBuilder BuildTestAndPackageTask { get; set; }
	//public static CakeTaskBuilder PackageBuildTask { get; set; }
	//public static CakeTaskBuilder PackageInstallTask { get; set; }
	//public static CakeTaskBuilder PackageVerifyTask { get; set; }
	public static CakeTaskBuilder PackageTestTask { get; set; }

	// Publishing
	public static CakeTaskBuilder PublishTask { get; set; }
	public static CakeTaskBuilder PublishToMyGetTask { get; set; }
	public static CakeTaskBuilder PublishToNuGetTask { get; set; }
	public static CakeTaskBuilder PublishToChocolateyTask { get; set; }
    public static CakeTaskBuilder PublishToLocalFeedTask { get; set; }
	public static CakeTaskBuilder PublishSymbolsPackageTask { get; set; }

	// Releasing
	public static CakeTaskBuilder CreateDraftReleaseTask { get; set; }
	//public static CakeTaskBuilder DownloadDraftReleaseTask { get; set; }
	//public static CakeTaskBuilder UpdateReleaseNotesTask { get; set; } 
	public static CakeTaskBuilder CreateProductionReleaseTask { get; set; }

	// Continuous Integration
	public static CakeTaskBuilder ContinuousIntegrationTask { get; set; }
}

// The following statements initialize each of the defined tasks as the file
// is loaded. They define what each of the tasks in the recipe actually does.
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
    .Does(() => {
        NuGetRestore(BuildSettings.SolutionFile, new NuGetRestoreSettings()
        {
            Source = new string[]   {
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
    .Does(() => {
        if (BuildSettings.BuildWithMSBuild)
            MSBuild(BuildSettings.SolutionFile, BuildSettings.MSBuildSettings);
        else
            DotNetBuild(BuildSettings.SolutionFile, BuildSettings.DotNetBuildSettings);
    });

BuildTasks.UnitTestTask = Task("Test")
    .Description("Run unit tests")
    .IsDependentOn("Build")
    .WithCriteria(() => !CommandLineOptions.NoTests)
    .Does(() => UnitTesting.RunAllTests());

BuildTasks.PackageTask = Task("Package")
    .IsDependentOn("Build")
    .Description("Build, Install, Verify and Test all packages")
    .Does(() => {
        foreach (var package in BuildSettings.SelectedPackages)
            package.BuildVerifyAndTest();
    });

BuildTasks.PackageTestTask = Task("PackageTest")
    .Description("Test all packages, which must already be built and installed.")
    .Does(() =>
    {
        foreach (var package in BuildSettings.SelectedPackages)
            package.RunPackageTests();
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

BuildTasks.PublishToMyGetTask = Task("PublishToMyGet")
    .Description("Publish or Re-publish any packages for MyGet")
    .WithCriteria(() => BuildSettings.IsLocalBuild)
    .Does(() => {
        if (!BuildSettings.ShouldPublishToMyGet)
            Information("Nothing to publish to MyGet from this run.");
        else if (CommandLineOptions.NoPush)
            Information("NoPush option suppressing publication to MyGet");
        else
            foreach (var package in BuildSettings.Packages)
                if (package.PackageType == PackageType.NuGet)
                    PackageReleaseManager.PushNuGetPackage(package.PackageFilePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
                else if (package.PackageType == PackageType.Chocolatey)
                    PackageReleaseManager.PushChocolateyPackage(package.PackageFilePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
    });

BuildTasks.PublishToNuGetTask = Task("PublishToNuGet")
    .Description("Publish or Re-publish any packages for NuGet")
    .WithCriteria(() => BuildSettings.IsLocalBuild)
    .Does(() => {
        if (!BuildSettings.ShouldPublishToNuGet)
            Information("Nothing to publish to NuGet from this run.");
        else if (CommandLineOptions.NoPush)
            Information("NoPush option suppressing publication to NuGet");
        else
            foreach (var package in BuildSettings.Packages)
                if (package.PackageType == PackageType.NuGet)
                    PackageReleaseManager.PushNuGetPackage(package.PackageFilePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
    });

BuildTasks.PublishToChocolateyTask = Task("PublishToChocolatey")
    .Description("Publish or Re-publish any packages for Chocolatey")
    .WithCriteria(() => BuildSettings.IsLocalBuild)
    .Does(() => {
        if (!BuildSettings.ShouldPublishToChocolatey)
            Information("Nothing to publish to Chocolatey from this run.");
        else if (CommandLineOptions.NoPush)
            Information("NoPush option suppressing publication to Chocolatey");
        else
            foreach (var package in BuildSettings.Packages)
                if (package.PackageType == PackageType.Chocolatey)
                    PackageReleaseManager.PushChocolateyPackage(package.PackageFilePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
    });

BuildTasks.PublishToLocalFeedTask = Task("PublishToLocalFeed")
    .Description("""
	Publishes packages to the local feed for a dev, alpha, beta, or rc build
	or for a final release. If not, or if the --nopush option was used,
	a message is displayed.
	""")
    .WithCriteria(() => BuildSettings.IsLocalBuild)
    .Does(() => {
        if (!BuildSettings.ShouldPublishToLocalFeed)
            Information("Nothing to add to local feed from this run.");
        else if (CommandLineOptions.NoPush)
            Information("NoPush option suppressing publication to local feed");
        else if (!SIO.Directory.Exists(BuildSettings.LocalPackagesDirectory))
            throw new Exception("Local packages directory not found");
        else
            foreach (var package in BuildSettings.Packages)
                if (package.PackageType == PackageType.NuGet || package.PackageType == PackageType.Chocolatey)
                    PackageReleaseManager.AddPackageToLocalFeed(package);
    });

BuildTasks.PublishSymbolsPackageTask = Task("PublishSymbolsPackage")
    .Description("Re-publish a specific symbols package to NuGet after a failure")
    .Does(() => PackageReleaseManager.PublishSymbolsPackage());

BuildTasks.CreateDraftReleaseTask = Task("CreateDraftRelease")
    .Description("Create a draft release on GitHub")
    .Does(() => PackageReleaseManager.CreateDraftRelease());

BuildTasks.CreateProductionReleaseTask = Task("CreateProductionRelease")
    .Description("Create a production GitHub Release")
    .Does(() => PackageReleaseManager.CreateProductionRelease());

BuildTasks.ContinuousIntegrationTask = Task("ContinuousIntegration")
    .Description("Perform continuous integration run")
    .IsDependentOn("DumpSettings")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Publish")
    .IsDependentOn("CreateDraftRelease")
    .IsDependentOn("CreateProductionRelease");

