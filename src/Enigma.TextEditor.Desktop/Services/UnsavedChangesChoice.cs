namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// What the user answered when asked about unsaved changes.
/// </summary>
public enum UnsavedChangesChoice
{
    /// <summary>
    /// Keep everything as it is: nothing is closed.
    /// </summary>
    Cancel,

    /// <summary>
    /// Save the changes, then close.
    /// </summary>
    Save,

    /// <summary>
    /// Throw the changes away and close.
    /// </summary>
    Discard,
}
