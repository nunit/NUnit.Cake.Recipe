public class NuGetPackage : PackageDefinition
{
    public NuGetPackage(
        string id, 
        string source, 
        string basePath = null,
        IPackageTestRunner testRunner = null,
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        PackageCheck[] symbols = null, 
        IEnumerable<PackageTest> tests = null)
    : base(
        PackageType.NuGet, 
        id, 
        source, 
        basePath: basePath,
        testRunner: testRunner, 
        testRunners: testRunners,
        checks: checks, 
        symbols: symbols, 
        tests: tests)
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

    public override void BuildPackage()
    {
        var NuGetPackSettings = new NuGetPackSettings()
        {
            Version = PackageVersion,
            BasePath = BasePath,
            OutputDirectory = BuildSettings.PackageDirectory,
            NoPackageAnalysis = true,
            Symbols = HasSymbols,
            Verbosity = BuildSettings.NuGetVerbosity
        };

        if (HasSymbols)
            NuGetPackSettings.SymbolPackageFormat = "snupkg";

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
                }.WithProperty("Version", BuildSettings.PackageVersion));
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
