// Representation of an extension, for use by PackageTests. Because our
// extensions usually exist as both nuget and chocolatey packages, each
// extension may have a nuget id, a chocolatey id or both. A default version
// is used unless the user overrides it using SetVersion.
public class ExtensionSpecifier
{
	public ExtensionSpecifier(string nugetId, string chocoId, string version)
	{
		NuGetId = nugetId;
		ChocoId = chocoId;
		Version = version;
	}

	public string NuGetId { get; }
	public string ChocoId { get; }
	public string Version { get; }

	public PackageReference NuGetPackage => new PackageReference(NuGetId, Version);
	public PackageReference ChocoPackage => new PackageReference(ChocoId, Version);
	public PackageReference LatestChocolateyRelease => ChocoPackage.LatestRelease;
	
	// Return an extension specifier using the same package ids as this
	// one but specifying a particular version to be used.
	public ExtensionSpecifier SetVersion(string version)
	{
		return new ExtensionSpecifier(NuGetId, ChocoId, version);
	}

	// Install this extension for a package
	public void InstallExtension(PackageDefinition targetPackage)
	{
		PackageReference extensionPackage = targetPackage.PackageType == PackageType.Chocolatey
			? ChocoPackage
			: NuGetPackage;
		
		extensionPackage.Install(targetPackage.ExtensionInstallDirectory);
	}
}
