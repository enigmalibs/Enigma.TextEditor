using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// The collection every test that touches the process-wide unhandled-exception hooks belongs to: their
/// state is the process's, so those tests never run concurrently.
/// </summary>
[CollectionDefinition(Name)]
public sealed class UnhandledExceptionHooksCollection
{
    /// <summary>
    /// The collection's name.
    /// </summary>
    public const string Name = "unhandled-exception-hooks";
}
