public class ZipPackage : PackageDefinition
{
    public ZipPackage(
        string id, 
        string source, 
        string packageVersion = null,
        IPackageTestRunner testRunner = null,
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null,
        PackageReference[] bundledExtensions = null )
    : base(
        PackageType.Zip, 
        id, 
        source, 
        packageVersion: packageVersion,
        testRunner: testRunner,
        testRunners: testRunners,
        checks: checks, 
        tests: tests) 
    {
        BundledExtensions = bundledExtensions;
    }

    // The file name of this package, including extension
    public override string PackageFileName => $"{PackageId}-{PackageVersion}.zip";
    // The directory into which this package is installed
    public override string PackageInstallDirectory => BuildSettings.ZipTestDirectory;
    // The directory used to contain results of package tests for this package
    public override string PackageResultDirectory => $"{BuildSettings.ZipResultDirectory}{PackageId}/";
    // The directory into which extensions to the test runner are installed
    public override string ExtensionInstallDirectory => $"{BuildSettings.ZipTestDirectory}{PackageId}.{PackageVersion}/bin/addins/";
    
    public override void BuildPackage()
    {
        FetchBundledExtensions(BundledExtensions);

        CreateZipImage();

        _context.Zip(BuildSettings.ZipImageDirectory, $"{BuildSettings.PackageDirectory}{PackageFileName}");
    }

    public override void InstallPackage()
    {
        var packageInstallationDir = $"{PackageInstallDirectory}{PackageId}.{PackageVersion}";
        _context.CleanDirectory(packageInstallationDir);
        _context.Unzip($"{BuildSettings.PackageDirectory}{PackageFileName}", packageInstallationDir);
    }

    protected override void InstallExtensions(ExtensionSpecifier[] extensionsNeeded)
    {
        // TODO: Do nothing for now. Future: only install extensions not bundled
        // or previously installed.
    }

    private void CreateZipImage()
    {
        _context.CleanDirectory(BuildSettings.ZipImageDirectory);

        _context.CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            BuildSettings.ZipImageDirectory);

        _context.CopyDirectory(
            BuildSettings.OutputDirectory,
            BuildSettings.ZipImageDirectory + "bin/" );

        _context.CopyDirectory(BuildSettings.ExtensionsDirectory, BuildSettings.ZipImageDirectory);
    }

    protected override bool IsRemovableExtension(string id)
    {
        return
            id.StartsWith("NUnit.Extension.") &&
            !id.StartsWith(PackageId) &&
            !id.Contains("PluggableAgent");
    }
}
