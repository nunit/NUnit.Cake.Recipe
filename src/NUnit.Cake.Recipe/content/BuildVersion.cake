public class BuildVersion
{
    // Prefixes for special types of branches
    private const string LOCAL_BRANCH_PREFIX = "local-";

    // NOTE: This is complicated because (1) the user may have specified 
    // the package version on the command-line and (2) GitVersion may
    // or may not be available. We'll work on solving (2) by getting
    // GitVersion to run for us on Linux, but (1) will alwas remain.
    //
    // We simplify things a by figuring out the full package version and
    // then parsing it to provide information that is used in the build.
    public BuildVersion(ICakeContext context)
    {
         if (context==null)
            throw new ArgumentNullException(nameof(context));

        BranchName = context.GitBranchCurrent(BuildSettings.ProjectDirectory).FriendlyName;
        IsLocalBranch = BranchName.StartsWith(LOCAL_BRANCH_PREFIX);

        // NOTE: The version of a Release Branch does not affect the PackageVersion
        // because it is only used for creating a draft release. On the other hand,
        // the version of a Local Branch is used directly as the Package Version.
        string packageVersion = CommandLineOptions.PackageVersion.Value ??
            (IsLocalBranch ? BranchName.Substring(LOCAL_BRANCH_PREFIX.Length) : context.MinVer().Version);
        
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
    public bool IsReleaseBranch { get; }
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
