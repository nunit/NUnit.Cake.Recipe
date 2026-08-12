public class PackageTestReport
{
	public PackageTest Test;
	public ActualResult Result;
	public IPackageTestRunner Runner;
	public List<string> Errors = new List<string>();
	public List<string> Warnings = new List<string>();

    public PackageTestReport(PackageTest test, ActualResult actualResult, IPackageTestRunner runner = null)
	{
		Test = test;
		Result = actualResult;
		Runner = runner;

		var expectedResult = test.ExpectedResult;
		var expectedOutput = test.ExpectedOutput;

		if (expectedResult != null)
		{
			if (actualResult.OverallResult == null)
				Errors.Add("   The test-run element has no result attribute.");
			else if (expectedResult.OverallResult != actualResult.OverallResult)
				Errors.Add($"   Expected: Overall Result = {expectedResult.OverallResult} But was: {actualResult.OverallResult}");

			CheckCounter("Test Count", expectedResult.Total, actualResult.Total);
			CheckCounter("Passed", expectedResult.Passed, actualResult.Passed);
			CheckCounter("Failed", expectedResult.Failed, actualResult.Failed);
			CheckCounter("Warnings", expectedResult.Warnings, actualResult.Warnings);
			CheckCounter("Inconclusive", expectedResult.Inconclusive, actualResult.Inconclusive);
			CheckCounter("Skipped", expectedResult.Skipped, actualResult.Skipped);

			var expectedAssemblies = expectedResult.Assemblies;
			var actualAssemblies = actualResult.Assemblies;

			for (int i = 0; i < expectedAssemblies.Length && i < actualAssemblies.Length; i++)
			{
				var expected = expectedAssemblies[i];
				var actual = actualAssemblies[i];

				if (expected.AssemblyName != actual.AssemblyName)
					Errors.Add($"   Expected: {expected.AssemblyName} But was: {actual.AssemblyName}");
				else if (runner == null || runner.PackageId == "NUnit.ConsoleRunner.NetCore")
				{
					if (actual.Runtime == null)
						Warnings.Add($"Unable to determine actual runtime used for {expected.AssemblyName}");
					else if (expected.Runtime != actual.Runtime)
						Errors.Add($"   Assembly {actual.AssemblyName} Expected: {expected.Runtime} But was: {actual.Runtime}");
				}
			}

			for (int i = actualAssemblies.Length; i < expectedAssemblies.Length; i++)
				Errors.Add($"   Assembly {expectedAssemblies[i].AssemblyName} was not found");

			for (int i = expectedAssemblies.Length; i < actualAssemblies.Length; i++)
				Errors.Add($"   Found unexpected assembly {actualAssemblies[i].AssemblyName}");
		}

		if (expectedOutput != null)
		{
			var output = runner.Output;
			if (output is not null)
				foreach (var outputCheck in expectedOutput)
				{
					if (!outputCheck.Matches(output))
						Errors.Add(outputCheck.Message);
				}
			else
				Errors.Add("No output was produced");
		}
    }

	public PackageTestReport(PackageTest test, int rc, IPackageTestRunner runner = null)
	{
		Test = test;
		Result = null;
		Runner = runner;

		if (rc != test.ExpectedReturnCode)
			Errors.Add($"   Expected: rc = {test.ExpectedReturnCode} But was: {rc}");
		else if (test.ExpectedOutput != null)
            foreach (var outputCheck in test.ExpectedOutput)
            {
                if (!outputCheck.Matches(runner.Output))
				Errors.Add(outputCheck.Message);
			}
	}

	public PackageTestReport(PackageTest test, Exception ex, IPackageTestRunner runner = null)
	{
		Test = test;
		Result = null;
        Runner = runner;

        Errors.Add($"     {ex.Message}");
    }

    public void Display(int index, TextWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine($"{index}. {Test.Description}");
		if (Runner != null)
		    writer.WriteLine($"   Runner: {Runner.PackageId} {Runner.Version}");
		writer.WriteLine($"   Args: {Test.Arguments}");
		writer.WriteLine();

		foreach (var error in Errors)
			writer.WriteLine(error);

		if (Errors.Count == 0)
		{
			writer.WriteLine("   SUCCESS: Test Result matches expected result!");
		}
		else
		{
			writer.WriteLine();
			writer.WriteLine("   ERROR: Test Result not as expected!");
		}

		foreach (var warning in Warnings)
			writer.WriteLine("   WARNING: " + warning);
	}

	private void CheckCounter(string label, int expected, int actual)
	{
		// If expected value of counter is negative, it means no check is needed
		if (expected >= 0 && expected != actual)
			Errors.Add($"   Expected: {label} = {expected} But was: {actual}");
	}

	private string GetAttribute(XmlNode node, string name)
	{
		return node.Attributes[name]?.Value;
	}
}

public class ResultReporter
{
	private string _packageName;
	private List<PackageTestReport> _reports = new List<PackageTestReport>();

	public ResultReporter(string packageName)
	{
		_packageName = packageName;
	}

	public void AddReport(PackageTestReport report)
	{
		_reports.Add(report);
	}

	public bool ReportResults(TextWriter writer)
	{
		writer.WriteLine("\n==============================================================================");
		writer.WriteLine($"Test Results for {_packageName}");
		writer.WriteLine("==============================================================================");

		writer.WriteLine("\nTest Environment");
		writer.WriteLine($"   OS Version: {Environment.OSVersion.VersionString}");
		writer.WriteLine($"  CLR Version: {Environment.Version}\n");

		int index = 0;
		bool hasErrors = false;

		foreach (var report in _reports)
		{
			hasErrors |= report.Errors.Count > 0;
			report.Display(++index, writer);
		}

		return hasErrors;
	}
}
