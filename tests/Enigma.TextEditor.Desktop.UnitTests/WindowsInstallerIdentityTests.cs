using System;
using System.IO;
using Enigma.TextEditor.Core;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests;

/// <summary>
/// The identity <c>packaging/windows/buildmsi.ps1</c> gives the Windows installer: the upgrade code, the product
/// name and the executable the shortcuts start. Nothing in the build reads that script, so a typo in any of them
/// would only show once an MSI is installed. These tests pin the three lines the script derives the rest from.
/// </summary>
public sealed class WindowsInstallerIdentityTests
{
    /// <summary>
    /// The upgrade code of the MSI that installed 1.9.0, read from its package. Every later MSI must keep it: one
    /// with another code installs beside the old version instead of replacing it.
    /// </summary>
    private const string UpgradeCode = "9f063d09-9f4e-4436-8cb3-39109eda380d";

    [Fact]
    public void TheInstallerKeepsTheUpgradeCodeOfTheInstalledReleases()
        => Assert.Contains($"$UpgradeCode = '{UpgradeCode}'", ReadScript());

    [Fact]
    public void TheInstallerNamesTheProductAsTheApplicationDoes()
        => Assert.Contains($"$ProductName = '{AppInfo.DisplayName}'", ReadScript());

    [Fact]
    public void TheInstallerPackagesAndStartsTheExecutableOfTheAssembly()
        => Assert.Contains($"$Assembly = '{typeof(App).Assembly.GetName().Name}'", ReadScript());

    private static string[] ReadScript()
        => File.ReadAllLines(Path.Combine(Root(), "packaging", "windows", "buildmsi.ps1"));

    /// <summary>
    /// The repository's root: the first directory above the test's output that holds the solution.
    /// </summary>
    private static string Root()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Enigma.TextEditor.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
