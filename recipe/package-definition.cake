public enum PackageType
{
    NuGet,
    Chocolatey,
    Zip
}

public abstract class PackageDefinition
{
    protected ICakeContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="packageType">A PackageType value specifying one of the four known package types</param>
    /// <param name="id">A string containing the package ID, used as the root of the PackageName</param>
    /// <param name="source">A string representing the source used to create the package, e.g. a nuspec file</param>
    /// <param name="testRunner">A TestRunner instance used to run package tests.</param>
    /// <param name="checks">An array of PackageChecks be made on the content of the package. Optional.</param>
    /// <param name="symbols">An array of PackageChecks to be made on the symbol package, if one is created. Optional. Only supported for nuget packages.</param>
    /// <param name="tests">An array of PackageTests to be run against the package. Optional.</param>
	protected PackageDefinition(
        PackageType packageType,
        string id,
        string source,
        string basePath = null, // Defaults to OutputDirectory
        IPackageTestRunner testRunner = null,
        IPackageTestRunner[] testRunners = null,
        string extraTestArguments = null,
        PackageCheck[] checks = null,
        PackageCheck[] symbols = null,
        IEnumerable<PackageTest> tests = null)
    {
        if (testRunner == null && testRunners == null && tests != null)
            throw new System.InvalidOperationException($"Unable to create {packageType} package {id}: TestRunner or TestRunners must be provided if there are tests.");
        if (testRunner != null && testRunners != null)
            throw new System.InvalidOperationException($"Unable to create {packageType} package {id}: Either TestRunner or TestRunners must be provided, but not both.");

        _context = BuildSettings.Context;

        PackageType = packageType;
        PackageId = id;
        // HACK
        //PackageVersion = packageType == PackageType.Chocolatey ? BuildSettings.ChocolateyPackageVersion : BuildSettings.PackageVersion;
        PackageVersion = BuildSettings.PackageVersion;
        PackageSource = source;
        BasePath = basePath ?? BuildSettings.OutputDirectory;
        TestRunner = testRunner;
        TestRunners = testRunners;
        ExtraTestArguments = extraTestArguments;
        PackageChecks = checks;
        SymbolChecks = symbols;
        PackageTests = tests;
    }

    public PackageType PackageType { get; }
	public string PackageId { get; }
    public string PackageVersion { get; protected set; }
	public string PackageSource { get; }
    public string BasePath { get; }

    // Defaults to null unless the package sets it.
    public PackageReference[] BundledExtensions { get; protected set; } = null;
    public bool HasBundledExtensions => BundledExtensions != null;

    public IPackageTestRunner TestRunner { get; }
    public IPackageTestRunner[] TestRunners { get; }
    public string ExtraTestArguments { get; }
    public PackageCheck[] PackageChecks { get; }
    public PackageCheck[] SymbolChecks { get; protected set; }
    public IEnumerable<PackageTest> PackageTests { get; }

    public bool HasSymbols { get; protected set; } = false;
    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for {PackageType} packages.");

    // The file name of this package, including extension
    public abstract string PackageFileName { get; }
    // The directory into which this package is installed
    public abstract string PackageInstallDirectory { get; }
    // The directory used to contain results of package tests for this package
    public abstract string PackageResultDirectory { get; }
    // The directory into which extensions to the test runner are installed
    public abstract string ExtensionInstallDirectory { get; }
    // The directory containing the package executable after installation
    public virtual string PackageTestDirectory => $"{PackageInstallDirectory}{PackageId}.{PackageVersion}/";

    public string PackageFilePath => BuildSettings.PackageDirectory + PackageFileName;

    public bool IsSelectedBy(string selectionExpression)
    {
        return IsSelectedByAny(selectionExpression.Split("|", StringSplitOptions.RemoveEmptyEntries));

        bool IsSelectedByAny(string[] terms)
        {
            foreach (var term in terms)
                if (IsSelectedByAll(term.Split("&", StringSplitOptions.RemoveEmptyEntries)))
                    return true;

            return false;
        }

        bool IsSelectedByAll(string[] factors)
        {
            foreach (string factor in factors)
            {
                int index = factor.IndexOf("=");
                if (index <= 0)
                    throw new ArgumentException("Selection expression does not contain =", "where");
                string prop = factor.Substring(0, index).Trim();
                if (factor[++index] == '=') ++index; // == operator
                string val = factor.Substring(index).Trim();

                switch(prop.ToUpper())
                {
                    case "ID":
                        return PackageId.ToLower() == val.ToLower();
                    case "TYPE":
                        return PackageType.ToString().ToLower() == val.ToLower();
                    default:
                        throw new Exception($"Not a valid selection property: {prop}");
                }
            }

            return false;
        }
    }

    public void BuildVerifyAndTest()
    {
        _context.EnsureDirectoryExists(BuildSettings.PackageDirectory);

        Banner.Display($"Building {PackageFileName}");
        BuildPackage();

        Banner.Display($"Installing {PackageFileName}");
        InstallPackage();

        if (PackageChecks != null)
        {
            Banner.Display($"Verifying {PackageFileName}");
            VerifyPackage();
        }

        if (SymbolChecks != null)
        {
            Banner.Display($"Verifying {SymbolPackageName}");
            // TODO: Override this in NuGetPackage
            VerifySymbolPackage();
        }

        if (PackageTests != null)
        {
            Banner.Display($"Testing {PackageFileName}");
            RunPackageTests();
        }
    }

    protected void FetchBundledExtensions(PackageReference[] extensions)
    {
        foreach (var extension in extensions)
            if (!extension.IsInstalled(BuildSettings.ExtensionsDirectory))
                extension.Install(BuildSettings.ExtensionsDirectory);
    }

    public abstract void BuildPackage();

    // This may be called by NuGet or Chocolatey packages
    public void AddPackageToLocalFeed()
    {
        try
        {
            _context.NuGetAdd(PackageFilePath, BuildSettings.LocalPackagesDirectory);
        }
        catch (Exception ex)
        {
            _context.Error(ex.Message);
        }
    }

    // Base implementation is used for installing both NuGet and
    // Chocolatey packages. Other package types should override.
    public virtual void InstallPackage()
    {
	    var installSettings = new NuGetInstallSettings
	    {
		    Source = new [] {
                // Package will be found here
                BuildSettings.PackageDirectory,
                // Dependencies may be in any of these
			    "https://www.myget.org/F/nunit/api/v3/index.json",
			    "https://api.nuget.org/v3/index.json" },
            Version = PackageVersion,
            OutputDirectory = PackageInstallDirectory,
            //ExcludeVersion = true,
		    Prerelease = true,
            Verbosity = BuildSettings.NuGetVerbosity,
            ArgumentCustomization = args => args.Append("-NoHttpCache")
	    };

        _context.NuGetInstall(PackageId, installSettings);
    }

    public void VerifyPackage()
    {
        bool allOK = true;

        if (PackageChecks != null)
            foreach (var check in PackageChecks)
                allOK &= check.ApplyTo(PackageTestDirectory);

        if (allOK)
            Console.WriteLine("All checks passed!");
        else 
            throw new Exception("Verification failed!");
    }

    public void RunPackageTests()
    {
        _context.Information($"Package tests will run at level {BuildSettings.PackageTestLevel}");

        var reporter = new ResultReporter(PackageFileName);

        _context.CleanDirectory(PackageResultDirectory);

        // Ensure we start out each package with no extensions installed.
        // If any package test installs an extension, it remains available
        // for subsequent tests of the same package only.
        foreach (DirectoryPath dirPath in _context.GetDirectories(ExtensionInstallDirectory + "*"))
        {
            string dirName = dirPath.Segments.Last();
            if (IsRemovableExtension(dirName))
            {
                _context.DeleteDirectory(dirPath, new DeleteDirectorySettings() { Recursive = true });
                _context.Information("Deleted directory " + dirPath.GetDirectoryName());
            }
        }

        // Package was defined with one or more TestRunners. These
        // may or may not require installation.
        var defaultRunners = TestRunners ?? new[] { TestRunner };

        // Preinstall all runners requiring installation
        InstallRunners(defaultRunners);

        foreach (var packageTest in PackageTests)
        {
            if (packageTest.Level > BuildSettings.PackageTestLevel)
                continue;

            InstallExtensions(packageTest.ExtensionsNeeded);

            // Use runners from the test if provided, otherwise the default runners
            var runners = packageTest.TestRunners is not null && packageTest.TestRunners.Length > 0
                ? packageTest.TestRunners
                : packageTest.TestRunner is not null
                    ? [packageTest.TestRunner]
                    : defaultRunners;
            InstallRunners(runners);

            foreach (var runner in runners)
            {
                Console.WriteLine(runner.Version);
                string testResultDir = $"{PackageResultDirectory}/{packageTest.Name}/";
                string resultFile = testResultDir + "TestResult.xml";

                Banner.Display(packageTest.Description);

                _context.CreateDirectory(testResultDir);
                string arguments = $"{packageTest.Arguments} {ExtraTestArguments} --work={testResultDir}";
                if (CommandLineOptions.TraceLevel.Value != "Off")
                    arguments += $" --trace:{CommandLineOptions.TraceLevel.Value}";
                bool redirectOutput = packageTest.ExpectedOutput != null;

                int rc = runner.RunPackageTest(arguments, redirectOutput);

                var actualResult = packageTest.ExpectedResult != null ? new ActualResult(resultFile) : null;

                try
                {
                    var report = new PackageTestReport(packageTest, actualResult, runner);
                    reporter.AddReport(report);

                    Console.WriteLine(report.Errors.Count == 0
                        ? "\nSUCCESS: Test Result matches expected result!"
                        : "\nERROR: Test Result not as expected!");
                }
                catch (Exception ex)
                {
                    reporter.AddReport(new PackageTestReport(packageTest, ex));

                    Console.WriteLine("\nERROR: No result found!");
                }

                //else
                //{
                //    var report = new PackageTestReport(packageTest, rc, runner);
                //    reporter.AddReport(report);

                //    if (rc != packageTest.ExpectedReturnCode)
                //        Console.WriteLine($"\nERROR: Expected rc = {packageTest.ExpectedReturnCode} but got {rc}!");
                //}
            }
        }

        // Create report as a string
        var sw = new StringWriter();
        bool hadErrors = reporter.ReportResults(sw);
        string reportText = sw.ToString();

        //Display it on the console
        Console.WriteLine(reportText);

        // Save it to the result directory as well
        using (var reportFile = new StreamWriter($"{PackageResultDirectory}/PackageTestSummary.txt"))
            reportFile.Write(reportText);

        if (hadErrors)
            throw new Exception("One or more package tests had errors!");
    }
    
    protected virtual void InstallExtensions(ExtensionSpecifier[] extensionsNeeded)
    {
        foreach (ExtensionSpecifier extension in extensionsNeeded)
            extension.InstallExtension(this);
    }

    protected abstract bool IsRemovableExtension(string dirName);

    private void InstallRunners(IEnumerable<IPackageTestRunner> runners)
    {
        // Install any runners needing installation
        foreach (var runner in runners)
            if (runner is InstallableTestRunner)
                InstallRunner((InstallableTestRunner)runner);
    }

    private void InstallRunner(InstallableTestRunner runner)
    {
        runner.Install(PackageInstallDirectory);

        foreach (var dependency in runner.Dependencies)
            dependency.InstallExtension(this);

        switch(PackageType)
        {
            case PackageType.Chocolatey:
		        // We are using nuget packages for the runner, so it won't normally recognize
		        // chocolatey extensions. We add an extra addins file and a VERIFICATION.txt file
                // for that purpose. The addins file is used in all releases up to 3.18. For
                // release 3.19 and higher, the VERIFICATION.txt file is checked to determine
                // whether this is a chocolatey package. Since the extra files do no harm
                // where they are not used, we create them in all cases.
                var addinsFile = runner.ExecutablePath.GetDirectory().CombineWithFilePath("choco.engine.addins").ToString();
                Console.WriteLine($"Creating {addinsFile}");

			    using (var writer = new StreamWriter(addinsFile))
			    {
				    writer.WriteLine("../../nunit-extension-*/tools/");
				    writer.WriteLine("../../nunit-extension-*/tools/*/");
                    writer.WriteLine("../../../nunit-extension-*/tools/");
                    writer.WriteLine("../../../nunit-extension-*/tools/*/");
                    writer.WriteLine("../../../../nunit-extension-*/tools/");
                    writer.WriteLine("../../../../nunit-extension-*/tools/*/");
                }

                var verificationFile = runner.ExecutablePath.GetDirectory().CombineWithFilePath("VERIFICATION.txt").ToString();
                using (var writer = new StreamWriter(verificationFile))
                {
                    writer.WriteLine("Simulated VERIFICATION file to force runner to use chocolatey extensions");
                }
                break;

            case PackageType.NuGet:
                // Special handling for testing under version 3.18
                if (runner.Version.StartsWith("3.18."))
                {
                    addinsFile = runner.ExecutablePath.GetDirectory().CombineWithFilePath("extra.nuget.engine.addins").ToString();
                    using (var writer = new StreamWriter(addinsFile))
                    {
                        writer.WriteLine("../../../../NUnit.Extension.*/tools/");
                        writer.WriteLine("../../../../NUnit.Extension.*/tools/*/");
                    }
                }
                break;
        }
    }

    public virtual void VerifySymbolPackage() { } // Does nothing. Overridden for NuGet packages.
}
