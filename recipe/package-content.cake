
public class PackageContent
{
	private ICakeContext _context;
	private List<FileSpec> _fileSpecs = new List<FileSpec>();

	public PackageContent()
	{
		_context = BuildSettings.Context;
	}

	public PackageContent(params DirectoryContent[] directories) : this(null, directories) { }

	public PackageContent(FilePath[] rootFiles = null, params DirectoryContent[] directories)
	{
		_context = BuildSettings.Context;

		RootFiles = rootFiles ?? new FilePath[0];
		Directories = directories;

		foreach (FilePath filePath in RootFiles)
			_fileSpecs.Add(new FileSpec(filePath));

		foreach (DirectoryContent content in Directories)
			foreach (FilePath source in content.Files)
				_fileSpecs.Add(new FileSpec(source, content.TargetDirectory));
	}

	public PackageContent WithRootFiles(params FilePath[] rootFiles)
	{
		RootFiles = rootFiles;

		foreach (FilePath filePath in RootFiles)
			_fileSpecs.Add(new FileSpec(filePath));

		return this;
	}

	public PackageContent WithDirectories(params DirectoryContent[] directories)
	{
		Directories = directories;

		foreach (DirectoryContent content in Directories)
			foreach (FilePath source in content.Files)
				_fileSpecs.Add(new FileSpec(source, content.TargetDirectory));

		return this;
	}

    public PackageContent WithDependencies(params PackageReference[] dependencies)
    {
        Dependencies = dependencies;
        return this;
    }

    public PackageContent WithDependencies(IEnumerable<PackageReference> dependencies)
    {
        Dependencies = dependencies.ToArray();
        return this;
    }

    public FilePath[] RootFiles { get; set; } = new FilePath[0];
	public DirectoryContent[] Directories { get; set; } = new DirectoryContent[0];
	public PackageReference[] Dependencies { get; set; } = new PackageReference[0];

	public List<NuSpecContent> GetNuSpecContent()
	{
		var result = new List<NuSpecContent>();

		foreach (FileSpec spec in _fileSpecs)
			result.Add(spec.AsNuSpecContent());

		return result;
	}

	// NOTE: Chocolatey doesn't support BasePath, so we have to add it ourselves
	public List<ChocolateyNuSpecContent> GetChocolateyNuSpecContent(string basePath)
	{
		var result = new List<ChocolateyNuSpecContent>();

		foreach (FileSpec spec in _fileSpecs)
			result.Add(spec.AsChocolateyNuSpecContent(basePath));

		return result;
	}

	public bool VerifyInstallation(DirectoryPath installDirectory, bool listFiles = false)
	{
		_context.Information("PackageContent Automatic Verification");
		bool isOK = true;

		// Checking the list of FileSpecs verifies both RootFiles and Directories
		foreach(FileSpec spec in _fileSpecs)
		{
			var fileName = spec.Source.GetFilename();
			var dirPath = spec.Target == null ? installDirectory : installDirectory.Combine(spec.Target);

			if (!_context.FileExists(dirPath.CombineWithFilePath(fileName)))
			{
				RecordError($"File {fileName} was not found.");
				isOK = false;
			}
            else if (listFiles)
                _context.Information($"    {fileName}");
        }

		_context.Information(isOK
			? "  Verification Sucessful!"
			: "  Verification Failed!");

        return isOK;
	}

    public static void RecordError(string msg)
    {
        Console.WriteLine("  ERROR: " + msg);
    }

	// Nested Class to hold file specifications
	private class FileSpec
	{
		public FileSpec(FilePath source, DirectoryPath target=null)
		{
			Source = source;
			Target = target;
		}

		public FilePath Source { get; }
		public DirectoryPath Target { get; }

		public NuSpecContent AsNuSpecContent() =>
			new NuSpecContent { Source = Source.ToString(), Target = Target?.ToString() };

		public ChocolateyNuSpecContent AsChocolateyNuSpecContent(string basePath) =>
			new ChocolateyNuSpecContent { Source = basePath + Source.ToString(), Target = Target?.ToString() };
	}
}

public class DirectoryContent
{
	private ICakeContext _context;

	public DirectoryContent(DirectoryPath relDirPath)
	{
		_context = BuildSettings.Context;
		TargetDirectory = relDirPath;
	}

	public DirectoryPath TargetDirectory { get; }
	public List<FilePath> Files { get; } = new List<FilePath>();

	public DirectoryContent WithFiles(params FilePath[] files)
	{
		Files.AddRange(files);
		return this;
	}

	public DirectoryContent AndFiles(params FilePath[] files)
	{
		return WithFiles(files);
	}

	public DirectoryContent WithFile(FilePath file)
	{
		Files.Add(file);
		return this;
	}

	public DirectoryContent AndFile(FilePath file)
	{
		return AndFiles(file);
	}
}