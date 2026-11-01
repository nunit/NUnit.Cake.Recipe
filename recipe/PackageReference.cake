// Representation of a package reference, containing everything needed to install it
public class PackageReference
{
	private static readonly string[] PRODUCTION_SOURCE = new [] {
		"https://www.nuget.org/api/v2/",
		"https://community.chocolatey.org/api/v2/"
	};

	private static readonly string[] DEVELOPMENT_SOURCE = new [] {
		"https://www.myget.org/F/nunit/api/v3/index.json"
	};	

	private ICakeContext _context;

	public string Id { get; }
	public string Version { get; }

	public PackageReference(string id, string version=null)
	{
		_context = BuildSettings.Context;

		Id = id;
		Version = version;
	}

	public PackageReference LatestStableRelease => GetLatestVersion(PRODUCTION_SOURCE, false);

	public PackageReference LatestReleaseIncludingPreReleases => GetLatestVersion(PRODUCTION_SOURCE, true);

	public PackageReference LatestDevBuild => GetLatestVersion(DEVELOPMENT_SOURCE, true);

	private PackageReference GetLatestVersion(string[] sources, bool includePrerelease)
	{
		var packageList = _context.NuGetList(Id, new NuGetListSettings()
		{
			Prerelease = includePrerelease, 
			Source = sources
		} );

		foreach (var package in packageList)
			if (package.Name == Id)
				return new PackageReference(package.Name, package.Version);

		return this;
	}

    public bool IsInstalled(string installDirectory)
	{
		return _context.GetDirectories($"{installDirectory}{Id}.*").Count > 0;
	}

	public void InstallExtension(PackageDefinition targetPackage)
	{
		Install(targetPackage.ExtensionInstallDirectory);
	}

	public void Install(string installDirectory)
	{
		if (!IsInstalled(installDirectory))
		{
			Banner.Display($"Installing {Id} version {Version}");

			var packageSources = new []
			{
				"https://www.myget.org/F/nunit/api/v3/index.json",
				"https://api.nuget.org/v3/index.json",
				"https://community.chocolatey.org/api/v2/",
				BuildSettings.LocalPackagesDirectory
			};

			Console.WriteLine("Package Sources:");
			foreach(var source in packageSources)
				Console.WriteLine($"  {source}");
			Console.WriteLine();

			_context.NuGetInstall(Id,
				new NuGetInstallSettings()
				{
					OutputDirectory = installDirectory,
					Version = Version,
					Source = packageSources
				});
		}
	}


	/*public IEnumerable<string> SearchNuGet(string id, string source)
	{
		var nuget = Context.Tools.Resolve("nuget.exe")
			?? throw new FileNotFoundException("nuget.exe not found");

		var exitCode = StartProcess(nuget, new ProcessSettings
		{
			Arguments = $"search {id} -Source {source} -Prerelease",
			RedirectStandardOutput = true,
			RedirectedStandardOutputHandler = (output) =>
			{
				if (!string.IsNullOrWhiteSpace(output))
				{
					var parts = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length >= 2)
					{
						yield return parts[0];
					}
				}
			}
		});

		if (exitCode != 0)
		{
			throw new Exception($"nuget search failed with exit code {exitCode}");
		}
	}*/
}
