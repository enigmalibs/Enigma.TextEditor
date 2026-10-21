using System;
using System.Linq;
using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// The requests that leave the engine: their spellings, vim's abbreviations of them, and everything else.
/// </summary>
/// <remarks>
/// Half of what these cases are for is the refusals. A parser that quietly read <c>:wqa</c> as
/// <c>:wq</c> or <c>:w!</c> as <c>:w</c> would be acting on a command the user meant something else
/// by, so every near miss is written down as one. <c>:q!</c> is the one former near miss that is now
/// read — as exactly what it says, which is the only reading that cannot cost work. The lines the
/// engine runs itself — <c>:s</c>, <c>:set</c>, a bare line number — are not file requests and come
/// back as nothing here; what they do is the engine's, and is held in <see cref="VimSubstituteTests"/>,
/// <see cref="VimSetTests"/> and <see cref="VimCommandLineTests"/>.
/// </remarks>
public sealed class ExCommandParserTests
{
    [Theory]
    [InlineData("w", VimExCommand.Write)]
    [InlineData("q", VimExCommand.Quit)]
    [InlineData("wq", VimExCommand.WriteQuit)]
    // The three spellings this phase adds. ':x' and ':wq!' are ':wq' to an editor whose save of an
    // unchanged buffer is a no-op, and ':q!' is its own request: discard, and do not ask.
    [InlineData("x", VimExCommand.WriteQuit)]
    [InlineData("wq!", VimExCommand.WriteQuit)]
    [InlineData("q!", VimExCommand.QuitDiscard)]
    public void TheSixSpellings_AreRead(string line, VimExCommand expected)
        => Assert.Equal(new VimExRequest(expected), ExCommandParser.Parse(line));

    [Theory]
    [InlineData(" w")]
    [InlineData("w ")]
    [InlineData("  wq  ")]
    [InlineData("\tq")]
    [InlineData(" q! ")]
    public void SurroundingWhitespace_IsASlipRatherThanADifferentCommand(string line)
        => Assert.NotNull(ExCommandParser.Parse(line));

    [Theory]
    // The near misses, each of which means something else in vim and none of which is implemented.
    // Read as the command it resembles, every one of them would write over a file the user did not
    // name or close tabs they did not name.
    [InlineData("w!")]
    [InlineData("x!")]
    [InlineData("wqa")]
    [InlineData("e!")]
    [InlineData("e! notes.md")]
    [InlineData("q notes.md")]
    [InlineData("bn 2")]
    [InlineData("write")]
    [InlineData("quit")]
    [InlineData("s/a/b/")]
    [InlineData("set ic")]
    [InlineData("42")]
    // A range on a file request: vim writes part of a file with ':1,3w', and there is no such thing
    // here.
    [InlineData("1,3w")]
    [InlineData("%q")]
    [InlineData("%wa")]
    [InlineData("1,2e x")]
    // Case matters: vim's own commands are lower case, and ':W' is a typo rather than ':w'.
    [InlineData("W")]
    [InlineData("Q")]
    [InlineData("X")]
    public void AnythingElse_IsNotACommand(string line)
        => Assert.Null(ExCommandParser.Parse(line));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyLine_IsNotACommandEither(string line)
        => Assert.Null(ExCommandParser.Parse(line));

    [Theory]
    [InlineData("wa", VimExCommand.WriteAll, null)]
    [InlineData("qa", VimExCommand.QuitAll, null)]
    [InlineData("qa!", VimExCommand.QuitAllDiscard, null)]
    [InlineData("qall!", VimExCommand.QuitAllDiscard, null)]
    [InlineData("bn", VimExCommand.BufferNext, null)]
    [InlineData("bp", VimExCommand.BufferPrevious, null)]
    [InlineData("e notes.md", VimExCommand.Edit, "notes.md")]
    [InlineData("edit C:/x y/z.txt", VimExCommand.Edit, "C:/x y/z.txt")]
    [InlineData("w notes.md", VimExCommand.Write, "notes.md")]
    public void TheRequestsAddedHere_AreReadWithTheirFile(string line, VimExCommand command, string? argument)
        => Assert.Equal(new VimExRequest(command, argument), ExCommandParser.Parse(line));

    [Theory]
    // Every spelling of ':edit' with nothing after it: the application asks which file.
    [InlineData("e")]
    [InlineData("ed")]
    [InlineData("edi")]
    [InlineData("edit")]
    [InlineData(" e ")]
    public void EditWithoutAFile_IsReadWithNoArgument(string line)
        => Assert.Equal(new VimExRequest(VimExCommand.Edit), ExCommandParser.Parse(line));

    [Theory]
    // Vim's abbreviations: any start of the full name from the shortest spelling on.
    [InlineData("vsp", VimExCommand.VerticalSplit, null)]
    [InlineData("vspl", VimExCommand.VerticalSplit, null)]
    [InlineData("vspli b.txt", VimExCommand.VerticalSplit, "b.txt")]
    [InlineData("spl", VimExCommand.Split, null)]
    [InlineData("spli notes.txt", VimExCommand.Split, "notes.txt")]
    [InlineData("ed notes.md", VimExCommand.Edit, "notes.md")]
    [InlineData("edi notes.md", VimExCommand.Edit, "notes.md")]
    [InlineData("bne", VimExCommand.BufferNext, null)]
    [InlineData("bnex", VimExCommand.BufferNext, null)]
    [InlineData("bpr", VimExCommand.BufferPrevious, null)]
    [InlineData("bprevio", VimExCommand.BufferPrevious, null)]
    [InlineData("wal", VimExCommand.WriteAll, null)]
    [InlineData("qal", VimExCommand.QuitAll, null)]
    [InlineData("qal!", VimExCommand.QuitAllDiscard, null)]
    [InlineData("vne", VimExCommand.VerticalNew, null)]
    [InlineData("onl", VimExCommand.Only, null)]
    [InlineData("clos", VimExCommand.Close, null)]
    [InlineData("h", VimExCommand.Help, null)]
    [InlineData("hel", VimExCommand.Help, null)]
    [InlineData("help", VimExCommand.Help, null)]
    [InlineData("help vsplit", VimExCommand.Help, "vsplit")]
    [InlineData("ene", VimExCommand.EditNew, null)]
    [InlineData("enew", VimExCommand.EditNew, null)]
    [InlineData("ene!", VimExCommand.EditNewDiscard, null)]
    [InlineData("enew!", VimExCommand.EditNewDiscard, null)]
    public void AShortenedName_IsReadAsVimReadsIt(string line, VimExCommand command, string? argument)
        => Assert.Equal(new VimExRequest(command, argument), ExCommandParser.Parse(line));

    [Theory]
    // Shorter than the shortest spelling, longer than the name, or not a start of it at all.
    [InlineData("v")]
    [InlineData("vn")]
    [InlineData("cl")]
    [InlineData("o")]
    [InlineData("b")]
    [InlineData("ne")]
    [InlineData("vsplitx")]
    [InlineData("splits")]
    [InlineData("vsplt")]
    // The rules a spelling does not change: no bang it does not take, no file where none is taken.
    [InlineData("vsp!")]
    [InlineData("help!")]
    [InlineData("helps")]
    [InlineData("clos notes.md")]
    [InlineData("ed!")]
    // ':en' is two letters short of ':ene', and ':enew' opens nothing named.
    [InlineData("en")]
    [InlineData("enews")]
    [InlineData("enew notes.md")]
    [InlineData("ene! notes.md")]
    public void ANameThatIsNotAStartOfOne_IsNotACommand(string line)
        => Assert.Null(ExCommandParser.Parse(line));

    [Fact]
    public void EverySpellingOfEveryCommand_NamesThatCommandAndNoOther()
    {
        foreach (VimExCommandDefinition definition in ExCommandParser.Definitions)
        {
            for (int length = definition.MinimumLength; length <= definition.Name.Length; length++)
            {
                string name = definition.Name[..length];

                Assert.Same(definition, Assert.Single(ExCommandParser.Definitions, candidate => candidate.Matches(name)));
            }
        }
    }

    [Fact]
    public void EveryRequest_HasADefinition()
    {
        // But SetFileOptions: ':set' is read by the engine itself (VimSet), which hands the file's options
        // on in that request; VimSetFileOptionsTests proves each spelling.
        foreach (VimExCommand command in Enum.GetValues<VimExCommand>().Where(command => command != VimExCommand.SetFileOptions))
        {
            Assert.Contains(
                ExCommandParser.Definitions,
                definition => (definition.Command == command) || (definition.BangCommand == command));
        }
    }

    [Theory]
    [InlineData("vsplit", false, ":vs[plit]")]
    [InlineData("w", false, ":w")]
    [InlineData("q", true, ":q!")]
    [InlineData("qall", true, ":qa[ll]!")]
    [InlineData("new", false, ":new")]
    [InlineData("vnew", false, ":vne[w]")]
    [InlineData("edit", false, ":e[dit]")]
    [InlineData("enew", true, ":ene[w]!")]
    public void ASpelling_BracketsThePartThatMayBeLeftOut(string name, bool bang, string spelling)
        => Assert.Equal(spelling, ExCommandParser.Definitions.Single(definition => definition.Name == name).Spelling(bang));

    [Fact]
    public void ALineIsRequired()
        => Assert.Throws<ArgumentNullException>(() => ExCommandParser.Parse(null!));
}
