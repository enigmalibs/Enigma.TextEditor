using System.Linq;
using Enigma.TextEditor.Desktop.Services;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

public sealed class LanguageCatalogTests
{
    private static readonly LanguageCatalog Catalog = new();

    [Theory]
    [InlineData("Program.cs", "C#", "source.cs")]
    [InlineData("data.json", "JSON", "source.json")]
    [InlineData("layout.xml", "XML", "text.xml")]
    [InlineData("README.md", "Markdown", "text.html.markdown")]
    [InlineData("script.py", "Python", "source.python")]
    [InlineData("SCRIPT.PY", "Python", "source.python")]
    public void AKnownExtension_PicksItsLanguage(string file, string name, string scope)
    {
        LanguageInfo language = Catalog.ForFile(file);

        Assert.Equal(name, language.DisplayName);
        Assert.Equal(scope, language.ScopeName);
    }

    [Theory]
    [InlineData("notes.unknownext")]
    [InlineData("no-extension")]
    [InlineData(null)]
    public void AnythingElse_IsPlainText(string? file)
        => Assert.Same(LanguageInfo.PlainText, Catalog.ForFile(file));

    [Fact]
    public void TheList_StartsWithPlainTextAndOffersOnlyLanguagesWithAGrammar()
    {
        Assert.Same(LanguageInfo.PlainText, Catalog.All[0]);
        Assert.All(Catalog.All.Skip(1), language => Assert.False(string.IsNullOrEmpty(language.ScopeName)));
        Assert.Equal(Catalog.All.Count, Catalog.All.Select(language => language.Id).Distinct().Count());
        Assert.True(Catalog.All.Count > 20, $"{Catalog.All.Count} languages");
    }

    [Fact]
    public void ALanguage_IsFoundByItsId()
    {
        Assert.Equal("C#", Catalog.Find("csharp")?.DisplayName);
        Assert.Same(LanguageInfo.PlainText, Catalog.Find("plaintext"));
        Assert.Null(Catalog.Find("no-such-language"));
    }
}
