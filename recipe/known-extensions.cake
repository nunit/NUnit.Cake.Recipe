// Static class holding information about known extensions.
public static class KnownExtensions
{
	// Static Variables representing well-known Extensions with the latest tested version.
	// Individual projects normally reference these without modification but may use
	// .SetVersion() in order to specify a different version, e.g. when testing a new one.
	public static ExtensionSpecifier NUnitV2Driver = new ExtensionSpecifier(
		"NUnit.Extension.NUnitV2Driver", "nunit-extension-nunit-v2-driver", "3.9.0");
	public static ExtensionSpecifier NUnitProjectLoader = new ExtensionSpecifier(
		"NUnit.Extension.NUnitProjectLoader", "nunit-extension-nunit-project-loader", "3.8.0");
	public static ExtensionSpecifier VSProjectLoader = new ExtensionSpecifier(
		"NUnit.Extension.VSProjectLoader", "nunit-extension-vs-project-loader", "3.9.0");
	public static ExtensionSpecifier NUnitV2ResultWriter = new ExtensionSpecifier(
		"NUnit.Extension.NUnitV2ResultWriter", "nunit-extension-nunit-v2-result-writer", "3.8.0");
	public static ExtensionSpecifier TeamCityEventListener = new ExtensionSpecifier(
		"NUnit.Extension.TeamCityEventListener", "nunit-extension-teamcity-event-listener", "1.0.9");
}

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
