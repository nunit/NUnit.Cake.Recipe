public class BuildVersion
{
    public BuildVersion(ICakeContext context, string requestedVersion)
    {
         if (context==null)
            throw new ArgumentNullException(nameof(context));

        // If a specific version is requested, we use that, otherwise get from MinVer.
        string packageVersion = requestedVersion ?? context.MinVer().Version;
        
        // Wherever we got it from, parse the package version
        int dash = packageVersion.IndexOf('-');
        IsPreRelease = dash > 0;

        string versionPart = packageVersion;
        string suffix = "";
        string label = "";

        if (IsPreRelease)
        {
            versionPart = packageVersion.Substring(0, dash);
            suffix = packageVersion.Substring(dash + 1);
            foreach (char c in suffix)
            {
                if (!char.IsLetter(c))
                    break;
                label += c;
            }
        }

        Version version = new Version(versionPart);
        SemVer = version.ToString(3);
        PreReleaseLabel = label;
        PreReleaseSuffix = suffix;

        PackageVersion = LegacyPackageVersion = packageVersion;
        AssemblyVersion = SemVer + ".0";
        AssemblyFileVersion = SemVer;
        AssemblyInformationalVersion = packageVersion;

        // We use a legacy SemVer 1.0 format for alpha, beta and rc releases on chocolatey
        var labelWithDot = label + ".";
        int num;
        if (suffix.StartsWith(labelWithDot) && int.TryParse(suffix.Substring(labelWithDot.Length), out num))
            LegacyPackageVersion = $"{SemVer}-{label}{num:000}";
    }

    public string BranchName { get; }
    public bool IsLocalBranch { get; }

    public string PackageVersion { get; }
    public string LegacyPackageVersion { get; }
    public string AssemblyVersion { get; }
    public string AssemblyFileVersion { get; }
    public string AssemblyInformationalVersion { get; }

    public string SemVer { get; }
    public bool IsPreRelease { get; }
    public string PreReleaseLabel { get; }
    public string PreReleaseSuffix { get; }
}
