using System;

namespace Enigma.TextEditor.Core.Instances;

/// <summary>
/// Where a running instance of the editor listens for files to open: its pipe, and its process.
/// </summary>
/// <param name="PipeName">The name of the pipe it serves.</param>
/// <param name="ProcessId">The process it runs in, which a launch checks is still alive before it connects.</param>
public sealed record InstanceEndpoint(string PipeName, int ProcessId)
{
    /// <summary>
    /// The endpoint of the process that is running.
    /// </summary>
    /// <returns>Its endpoint.</returns>
    public static InstanceEndpoint ForCurrentProcess() => ForProcess(Environment.ProcessId);

    /// <summary>
    /// The endpoint a process of the editor serves.
    /// </summary>
    /// <param name="processId">The process.</param>
    /// <returns>Its endpoint.</returns>
    /// <remarks>
    /// One pipe per process, named after it: two instances never compete for a name, and a pipe left behind by
    /// one that crashed belongs to no one else.
    /// </remarks>
    public static InstanceEndpoint ForProcess(int processId)
        => new($"{AppInfo.AppDataFolderName}.{processId}", processId);
}
