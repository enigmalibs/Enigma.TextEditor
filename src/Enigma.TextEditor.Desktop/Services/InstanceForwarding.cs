using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Instances;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The launch side of opening files in the running instance: a launch that names files hands them to the
/// instance used last, and ends, instead of opening a window of its own.
/// </summary>
/// <remarks>
/// <para>
/// Runs in <see cref="Program.Main"/>, before Avalonia, so a launch that hands its files over never shows a window.
/// A launch without files always starts an instance: several can run side by side, and that is how one is opened.
/// </para>
/// <para>
/// Every way it can fail — no instance, one that has gone, one that does not answer in time — ends in the launch
/// starting its own instance and opening the files there, as it always did. Nothing is lost.
/// </para>
/// </remarks>
internal static class InstanceForwarding
{
    /// <summary>
    /// Hands a launch's files to the running instance used last.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>Whether an instance took them, and this launch can end.</returns>
    /// <remarks>
    /// Blocks: it runs on the main thread before the application exists, where there is nothing else to do and no
    /// context to deadlock on, and for a few seconds at the very worst.
    /// </remarks>
    public static bool TryForward(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        StartupFiles startup = StartupFiles.Parse(args, Environment.CurrentDirectory, File.Exists, Directory.Exists);

        return TryForwardAsync(
                startup,
                AppPaths.ActiveInstanceFile,
                IsRunningEditor,
                (pipe, files) => InstanceClient.TryForwardAsync(pipe, files))
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Hands files to the instance a pointer file names, when it is another process of this editor that is running.
    /// </summary>
    /// <param name="startup">The launch's arguments, read.</param>
    /// <param name="pointerFile">The file naming the instance used last.</param>
    /// <param name="isRunning">Whether an instance's process is a running editor.</param>
    /// <param name="forward">Sends the files down a pipe; whether they were taken.</param>
    /// <returns>Whether they were taken.</returns>
    /// <remarks>
    /// Only the files go, as full paths: a folder named with them is not a file to open, and there is no one left to
    /// tell. The process is checked first, because reaching for a pipe nobody serves costs the whole connect timeout
    /// on Windows; and it is never this one, whose id a dead instance may have had.
    /// </remarks>
    internal static async Task<bool> TryForwardAsync(
        StartupFiles startup,
        string pointerFile,
        Func<InstanceEndpoint, bool> isRunning,
        Func<string, IReadOnlyList<string>, Task<bool>> forward)
    {
        ArgumentNullException.ThrowIfNull(startup);
        ArgumentNullException.ThrowIfNull(isRunning);
        ArgumentNullException.ThrowIfNull(forward);

        if ((startup.Files.Count == 0)
            || (ActiveInstanceFile.TryRead(pointerFile) is not { } endpoint)
            || (endpoint.ProcessId == Environment.ProcessId)
            || !isRunning(endpoint))
        {
            return false;
        }

        // Before the files go: the instance is asked to come to the front, which Windows only lets it do with
        // the permission of the process the user is working with — this one.
        if (OperatingSystem.IsWindows())
        {
            _ = AllowSetForegroundWindow(endpoint.ProcessId);
        }

        return await forward(endpoint.PipeName, [.. startup.Files.Select(file => file.Path)]);
    }

    // The same program as this one: a process id outlives its process, and may have been given to anything since.
    private static bool IsRunningEditor(InstanceEndpoint endpoint)
    {
        try
        {
            using Process other = Process.GetProcessById(endpoint.ProcessId);
            using Process self = Process.GetCurrentProcess();

            return !other.HasExited && string.Equals(other.ProcessName, self.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);
}
