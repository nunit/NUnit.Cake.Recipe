// Load all tools used by the recipe
#tool NuGet.CommandLine&version=6.9.1
#tool dotnet:?package=GitVersion.Tool&version=6.4.0
#tool dotnet:?package=GitReleaseManager.Tool&version=0.20.0
#addin nuget:?package=Cake.Git&version=5.0.1

// Using statements needed in the scripts
using Cake.Git;
using System.Text.RegularExpressions;
using System.Xml;
using SIO = System.IO;

public static class Tools
{
	public static DirectoryPath FindInstalledTool(string packageId)
	{
		if (SIO.Directory.Exists(BuildSettings.ToolsDirectory + packageId))
			return BuildSettings.ToolsDirectory + packageId;

		foreach(var dir in BuildSettings.Context.GetDirectories(BuildSettings.ToolsDirectory + $"{packageId}.*"))
			return dir; // Use first one found

		return null;
	}

	public static DirectoryPath FindInstalledTool(string packageId, string version)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		var toolPath = BuildSettings.ToolsDirectory + $"{packageId}.{version}";
		return BuildSettings.ToolsDirectory + $"{packageId}.{version}";
	}
}