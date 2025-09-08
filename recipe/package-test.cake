// Representation of a single test to be run against a pre-built package.
// Each test has a Level, with the following values defined...
//  0 Do not run - used for temporarily disabling a test
//  1 Run for all CI tests - that is every time we test packages
//  2 Run only on PRs, dev builds and when publishing
//  3 Run only when publishing
public class PackageTest
{
    public int Level { get; private set; }
    public string Name { get; private set; }

    public string Description { get; set; }
    public string Arguments { get; set; }
    public int ExpectedReturnCode { get; set; } = 0;
    public ExpectedResult ExpectedResult { get; set; }
    public OutputCheck[] ExpectedOutput { get; set; }
    public ExtensionSpecifier[] ExtensionsNeeded { get; set; } = new ExtensionSpecifier[0];
    public IPackageTestRunner TestRunner { get; set; }
    public IPackageTestRunner[] TestRunners { get; set; }

    public PackageTest(int level, string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        Level = level;
        Name = name;
        Description = name;
    }
}

public class MultipleRunnerPackageTest : PackageTest
{ 
	public MultipleRunnerPackageTest(int level, string name, string description, string arguments, ExpectedResult expectedResult, params IPackageTestRunner[] testRunners )
        : base(level, name)
    {
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
		TestRunners = testRunners;
    }
}
