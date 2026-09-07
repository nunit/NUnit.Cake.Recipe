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
        source: source, 
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

    public override void BuildPackage()
    {
        if (PackageSource.EndsWith(".csproj"))
            Dotnet.Execute($"pack \"{PackageSource}\" --version {BuildSettings.PackageVersion} --configuration {BuildSettings.Configuration} --output \"{BuildSettings.PackageDirectory}\"");
        else
            base.BuildPackage();
    }

    public override void InstallPackage()
    {
        Dotnet.Execute($"tool install {PackageId} --version {PackageVersion} --tool-path \"{PackageInstallDirectory}\"");
    }
}
