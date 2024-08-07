// A dotnet tool is packaged using nuget but requires a different installation step
public class DotNetToolPackage : NuGetPackage
{
    public DotNetToolPackage(
        string id, 
        string source, 
        string basePath = null,
        IPackageTestRunner testRunner = null,
        TestRunnerSource testRunnerSource = null,
        PackageCheck[] checks = null, 
        PackageCheck[] symbols = null, 
        IEnumerable<PackageTest> tests = null)
    : base(
        id, 
        source, 
        basePath: basePath,
        testRunner: testRunner, 
        testRunnerSource: testRunnerSource,
        checks: checks, 
        symbols: symbols, 
        tests: tests)
    {
    }

    public override void InstallPackage()
    {
        var arguments = $"tool install {PackageId} --version {BuildSettings.PackageVersion} " + 
            $"--add-source \"{BuildSettings.PackageDirectory}\" --tool-path \"{PackageTestDirectory}\"";
        Console.WriteLine($"Executing dotnet {arguments}");
        _context.StartProcess("dotnet", arguments);
    }
}
