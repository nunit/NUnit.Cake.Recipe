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
        if (!source.EndsWith(".nuspec"))
            throw new ArgumentException("Source must be a nuspec file", nameof(source));

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
        var nugetPackSettings = new NuGetPackSettings()
        {
            Version = PackageVersion,
            BasePath = BasePath,
            OutputDirectory = BuildSettings.PackageDirectory,
            NoPackageAnalysis = true,
            Symbols = HasSymbols,
            Verbosity = BuildSettings.NuGetVerbosity
        };

        if (HasSymbols)
            nugetPackSettings.SymbolPackageFormat = "snupkg";

        _context.NuGetPack(PackageSource, nugetPackSettings);
    }
}
