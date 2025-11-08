// A dotnet tool is packaged using nuget but requires a different installation step
public class DotNetToolPackage : NuGetPackage
{
    public DotNetToolPackage(
        string id, 
        string source, 
        string packageVersion = null,
        string basePath = null,
        IPackageTestRunner testRunner = null,
        IPackageTestRunner[] testRunners = null,
        PackageCheck[] checks = null, 
        PackageCheck[] symbols = null, 
        IEnumerable<PackageTest> tests = null)
    : base(
        id, 
        source, 
        packageVersion: packageVersion,
        basePath: basePath,
        testRunner: testRunner, 
        testRunners: testRunners,
        checks: checks, 
        symbols: symbols, 
        tests: tests)
    {
    }

    public override string PackageTestDirectory => PackageInstallDirectory;

    public override void InstallPackage()
    {
        var arguments = $"tool install {PackageId} --version {PackageVersion} " + 
            $"--add-source \"{BuildSettings.PackageDirectory}\" --tool-path \"{PackageInstallDirectory}\"";
        Console.WriteLine($"Executing dotnet {arguments}");
        _context.StartProcess("dotnet", arguments);
    }
}
