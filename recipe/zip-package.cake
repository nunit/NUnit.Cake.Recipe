public class ZipPackage : PackageDefinition
{
    public ZipPackage(
        string id, 
        string source, 
        IPackageTestRunner testRunner = null,
        TestRunnerSource testRunnerSource = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null,
        PackageReference[] bundledExtensions = null )
    : base(
        PackageType.Zip, 
        id, 
        source, 
        testRunner: testRunner,
        testRunnerSource: testRunnerSource,
        checks: checks, 
        tests: tests) 
    {
        BundledExtensions = bundledExtensions;
    }

    // ZIP package supports bundling of extensions
    public PackageReference[] BundledExtensions { get; }
    public bool HasBundledExtensions => BundledExtensions != null;

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
        _context.Unzip($"{BuildSettings.PackageDirectory}{PackageFileName}", $"{PackageInstallDirectory}{PackageId}.{PackageVersion}");
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

        var addinsDir = BuildSettings.ZipImageDirectory + "bin/net462/addins/";
        _context.CreateDirectory(addinsDir);

        foreach (var packageDir in SIO.Directory.GetDirectories(BuildSettings.ExtensionsDirectory))
        {
            var files = _context.GetFiles(packageDir + "/tools/*").Concat(_context.GetFiles(packageDir + "/tools/net462/*"));
            _context.CopyFiles(files.Where(f => f.GetExtension() != ".addins"), addinsDir);
        }
    }
}
