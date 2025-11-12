public class NuGetPackage : PackageDefinition
{
    public NuGetPackage(
        string id, 
        string title = null,
        string description = null,
        string summary = null,
        string[] releaseNotes = null,
        string[] tags = null,
        string source = null, 
        string packageVersion = null,
        string basePath = null,
        IPackageTestRunner testRunner = null,
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        PackageCheck[] symbols = null, 
        IEnumerable<PackageTest> tests = null,
        PackageContent packageContent = null)
    : base(
        PackageType.NuGet, 
        id,
        title: title,
        description: description,
        summary: summary,
        releaseNotes: releaseNotes,
        source: source, 
        packageVersion: packageVersion,
        basePath: basePath,
        testRunner: testRunner, 
        testRunners: testRunners,
        checks: checks, 
        symbols: symbols, 
        tests: tests,
        packageContent: packageContent)
    {
        //if (!source.EndsWith(".nuspec"))
        //    throw new ArgumentException("Source must be a nuspec file", nameof(source));

        if (symbols != null)
        {
            HasSymbols = true;
            SymbolChecks = symbols;
        }
    }

    // The file name of this package, including extension
    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    // The file name of any symbol package, including extension
    public override string SymbolPackageName => SIO.Path.ChangeExtension(PackageFileName, ".snupkg");
    // The directory into which this package is installed
    public override string PackageInstallDirectory => BuildSettings.NuGetTestDirectory;
    // The directory used to contain results of package tests for this package
    public override string PackageResultDirectory => BuildSettings.NuGetResultDirectory + PackageId + "/";
    // The directory into which extensions to the test runner are installed
    public override string ExtensionInstallDirectory => BuildSettings.NuGetTestDirectory;

    protected virtual NuGetPackSettings NuGetPackSettings
    {
        get
        {
            var repositoryUrl = NUNIT_GITHUB_URL + BuildSettings.GitHubRepository + "/";
            var rawGitHubUserContent = "https://raw.githubusercontent.com/" + BuildSettings.GitHubRepository + "/main/";

            var settings = new NuGetPackSettings()
            {
                // From PackageDefinition
                Id = PackageId,
                Version = PackageVersion,
                Title = PackageTitle ?? PackageId,
                Description = PackageDescription,
                ReleaseNotes = ReleaseNotes,
                Tags = Tags,
                BasePath = BasePath,
                // From BuildSettings
                Verbosity = BuildSettings.NuGetVerbosity,
                OutputDirectory = BuildSettings.PackageDirectory,
                Repository = new NuGetRepository() { Type = "Git", Url = repositoryUrl },
                Symbols = HasSymbols,
                // Common to all packages
                Authors = NUNIT_PACKAGE_AUTHORS,
                Copyright = NUNIT_COPYRIGHT,
                ProjectUrl = new Uri(NUNIT_PROJECT_URL),
                License = NUNIT_LICENSE,
                RequireLicenseAcceptance = false,
                Icon = NUNIT_ICON,
                Language = "en-US",
                NoPackageAnalysis = true,
            };

            if (HasSymbols)
                settings.SymbolPackageFormat = "snupkg";

            if (PackageContent != null)
            {
                foreach (var item in PackageContent.GetNuSpecContent())
                    settings.Files.Add(item);

                foreach (PackageReference dependency in PackageContent.Dependencies)
                    settings.Dependencies.Add(new NuSpecDependency { Id = dependency.Id, Version = dependency.Version });
            }

            return settings;
        }
    }

    public override void BuildPackage()
    {
        if (string.IsNullOrEmpty(PackageSource))
            _context.NuGetPack(NuGetPackSettings);
        else if (PackageSource.EndsWith(".nuspec"))
            _context.NuGetPack(PackageSource, NuGetPackSettings);
        else if (PackageSource.EndsWith(".csproj"))
            _context.MSBuild(PackageSource,
                new MSBuildSettings
                {
                    Target = "pack",
                    Verbosity = BuildSettings.MSBuildVerbosity,
                    Configuration = BuildSettings.Configuration,
                    PlatformTarget = PlatformTarget.MSIL,
                    //AllowPreviewVersion = BuildSettings.MSBuildAllowPreviewVersion
                }.WithProperty("Version", PackageVersion));
        else
            throw new ArgumentException(
                $"Invalid package source specified: {PackageSource}", "source");
    }

    public override void VerifySymbolPackage()
    {
        if (!SIO.File.Exists(BuildSettings.PackageDirectory + SymbolPackageName))
        {
            _context.Error($"  ERROR: File {SymbolPackageName} was not found.");
            throw new Exception("Verification Failed!");
        }

        string tempDir = SIO.Directory.CreateTempSubdirectory().FullName;
        _context.Unzip(BuildSettings.PackageDirectory + SymbolPackageName, tempDir);

        bool allOK = true;

        if (allOK && SymbolChecks != null)
            foreach (var check in SymbolChecks)
                allOK &= check.ApplyTo(tempDir);

        SIO.Directory.Delete(tempDir, true);

        if (allOK)
            Console.WriteLine("All checks passed!");
        else
            throw new Exception("Verification failed!");
    }

    protected override bool IsRemovableExtension(string id)
    {
        return
            id.StartsWith("NUnit.Extension.") &&
            !id.StartsWith(PackageId) &&
            !id.Contains("PluggableAgent");
    }
}
