using System;
using System.IO;
using System.Text.Json;

namespace Enigma.TextEditor.Core.Instances;

/// <summary>
/// The file naming the instance whose window was used last: the one a launch with files hands them to.
/// </summary>
/// <remarks>
/// Every instance writes itself in when its window is activated, so with several open the last one used wins.
/// The file is a pointer, not a lock: it may name an instance that has gone, which a launch finds out when it
/// cannot reach it.
/// </remarks>
public static class ActiveInstanceFile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Names an instance as the last one used, replacing whichever was.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="endpoint">The instance.</param>
    /// <exception cref="IOException">The file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file or its folder is not writable.</exception>
    /// <remarks>
    /// Written beside the file and moved over it, so a launch reading it at the same moment sees the old pointer or
    /// the new one, never half of one. The temporary name carries the process, as two instances may write at once.
    /// </remarks>
    public static void Write(string path, InstanceEndpoint endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        string temporary = $"{path}.{Environment.ProcessId}.tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(endpoint, Json));
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>
    /// Reads which instance was used last.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The instance, or <see langword="null"/> when the file is missing, unreadable or not a pointer.</returns>
    public static InstanceEndpoint? TryRead(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            return JsonSerializer.Deserialize<InstanceEndpoint>(File.ReadAllText(path), Json) is
                { PipeName: { Length: > 0 }, ProcessId: > 0 } endpoint
                ? endpoint
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Takes an instance's name out of the file, when it is still the one there: as it closes, so that no launch
    /// waits on it.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="endpoint">The instance.</param>
    /// <remarks>
    /// Another instance's pointer is left alone. Best effort: a file that cannot be read or deleted stays, and a
    /// launch that finds it pointing at nothing starts an instance of its own.
    /// </remarks>
    public static void Clear(string path, InstanceEndpoint endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (TryRead(path) != endpoint)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Left for the next writer to replace.
        }
    }
}
