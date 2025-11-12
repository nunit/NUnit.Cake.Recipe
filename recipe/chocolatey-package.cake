public class ChocolateyPackage : PackageDefinition
{
    public ChocolateyPackage(
        string id,
        string title = null,
        string summary = null,
        string description = null,
        string[] releaseNotes = null,
        string[] tags = null,
        string source = null,
        string packageVersion = null,
        IPackageTestRunner testRunner = null, 
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null,
        PackageContent packageContent = null )
    : base(
        PackageType.Chocolatey,
        id, 
        title: title,
        summary: summary,
        description: description,
        releaseNotes: releaseNotes,
        tags: tags,
        source: source,
        packageVersion: packageVersion,
        testRunner: testRunner, 
        testRunners: testRunners,
        checks: checks, 
        tests: tests,
        packageContent: packageContent )
    {
    }

    // The file name of this package, including extension
    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    // The file name of any symbol package, including extension
    public override string SymbolPackageName => SIO.Path.ChangeExtension(PackageFileName, ".snupkg");
    // The directory into which this package is installed
    public override string PackageInstallDirectory => BuildSettings.ChocolateyTestDirectory;
    // The directory used to contain results of package tests for this package
    public override string PackageResultDirectory => BuildSettings.ChocolateyResultDirectory + PackageId + "/";
    // The directory into which extensions to the test runner are installed
    public override string ExtensionInstallDirectory => BuildSettings.ChocolateyTestDirectory;
    
    protected virtual ChocolateyPackSettings ChocolateyPackSettings
    {
        get
        {
            var repositoryUrl = NUNIT_GITHUB_URL + BuildSettings.GitHubRepository + "/";
            var rawGitHubUserContent = "https://raw.githubusercontent.com/" + BuildSettings.GitHubRepository + "/main/";

            // NOTE: Because of how Cake build works, these settings will
            // override any settings in a nuspec file. Therefore, no settings
            // should be initialized unless they either
            //  1) are taken from the PackageDefinition itself.
            //  2) are taken from the BuildSettings, which apply to all packages being built.
            //  3) are defined to be the same for all TestCentric packages.

            var settings = new ChocolateyPackSettings
            {
                // From PackageDefinition
                Id = PackageId,
                Version = PackageVersion,
                Title = PackageTitle ?? PackageId,
                Summary = PackageSummary,
                Description = PackageDescription,
                ReleaseNotes = ReleaseNotes,
                Tags = Tags,
                // From BuildSettings
                LicenseUrl = new Uri($"{NUNIT_RAW_URL}{BuildSettings.GitHubRepository}/main/LICENSE.txt"),
                Verbose = BuildSettings.ChocolateyVerbosity,
                OutputDirectory = BuildSettings.PackageDirectory,
                ProjectSourceUrl = new Uri(repositoryUrl),
                PackageSourceUrl = new Uri(repositoryUrl),
                BugTrackerUrl = new Uri(repositoryUrl + "issues"),
                // Common to all packages
                Authors = NUNIT_PACKAGE_AUTHORS,
                Owners = NUNIT_PACKAGE_OWNERS,
                Copyright = NUNIT_COPYRIGHT,
                ProjectUrl = new Uri(NUNIT_PROJECT_URL),
                RequireLicenseAcceptance = false,
                DocsUrl = new Uri(NUNIT_PROJECT_URL),
                MailingListUrl = new Uri(NUNIT_MAILING_LIST_URL),
                ArgumentCustomization = args => args.Append($"BIN_DIR={BuildSettings.OutputDirectory}")
            };

            if (PackageContent != null)
            {
                foreach (var item in PackageContent.GetChocolateyNuSpecContent(BasePath))
                    settings.Files.Add(item);

                foreach (PackageReference dependency in PackageContent.Dependencies)
                    settings.Dependencies.Add(new ChocolateyNuSpecDependency { Id = dependency.Id, Version = dependency.Version });
            }

            return settings;
        }
    }
    
    public override void BuildPackage()
    {
        if (string.IsNullOrEmpty(PackageSource))
            _context.ChocolateyPack(ChocolateyPackSettings);
        else if (PackageSource.EndsWith(".nuspec"))
            _context.ChocolateyPack(PackageSource, ChocolateyPackSettings);
        else
            throw new ArgumentException(
                $"Invalid package source specified: {PackageSource}", "source");
    }

    protected override bool IsRemovableExtension(string id)
    {
        return
            id.StartsWith("nunit-extension-") &&
            !id.StartsWith(PackageId) &&
            !id.Contains("pluggable-agent");
    }
}
