public class RecipePackage : NuGetPackage
{
    private IEnumerable<FilePath> _cakeFiles;

    /// <summary>
    /// Construct passing all required arguments
    /// </summary>
    /// <param name="id">A string containing the package ID, used as the root of the PackageName.</param>
    /// <param name="basePath">Path used in locating binaries for the package.</param>
    /// <param name="checks">An array of PackageChecks be made on the content of the package. Optional.</param>
	public RecipePackage(
        string id,
        string source,
        string content = "recipe/*.cake",
        PackageCheck[] checks = null )
    : base (
        id, 
        source: source,
        basePath: BuildSettings.ProjectDirectory,
        checks: checks )
    {
        _cakeFiles = _context.GetFiles(content).Select(f => f.GetFilename());
    }

    public override void BuildPackage()
    {
        Console.WriteLine("BuildPackage override called");
        var settings = new NuGetPackSettings()
        {
            Version = PackageVersion,
            BasePath = BasePath,
            OutputDirectory = BuildSettings.PackageDirectory,
            NoPackageAnalysis = true,
            Symbols = HasSymbols
        };

        settings.Files.Add(new NuSpecContent() { Source="README.md" });
        settings.Files.Add(new NuSpecContent() { Source="LICENSE.txt" });
        settings.Files.Add(new NuSpecContent() { Source="nunit_256.png" });
        foreach (FilePath filePath in _cakeFiles)
            settings.Files.Add(new NuSpecContent() { Source=$"recipe/{filePath}", Target="content" });

        _context.Information("Calling NuGetPack");
        _context.NuGetPack(PackageSource, settings);
    }
}