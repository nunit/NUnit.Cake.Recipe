public class ChocolateyPackage : PackageDefinition
{
    public ChocolateyPackage(
        string id, 
        string source, 
        string packageVersion = null,
        IPackageTestRunner testRunner = null, 
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null)
    : base(
        PackageType.Chocolatey,
        id, 
        source, 
        packageVersion: packageVersion,
        testRunner: testRunner, 
        testRunners: testRunners,
        checks: checks, 
        tests: tests)
    {
        if (!source.EndsWith(".nuspec"))
            throw new ArgumentException("Source must be a nuspec file", nameof(source));
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
    
    public override void BuildPackage()
    {
        _context.ChocolateyPack(PackageSource,
            new ChocolateyPackSettings()
            {
                Version = PackageVersion,
                OutputDirectory = BuildSettings.PackageDirectory,
                ArgumentCustomization = args => args.Append($"BIN_DIR={BuildSettings.OutputDirectory}")
            });
    }

    protected override bool IsRemovableExtension(string id)
    {
        return
            id.StartsWith("nunit-extension-") &&
            !id.StartsWith(PackageId) &&
            !id.Contains("pluggable-agent");
    }
}
