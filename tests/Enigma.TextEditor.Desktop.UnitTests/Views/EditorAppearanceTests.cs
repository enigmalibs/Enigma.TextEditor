using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The editor's colouring: an editor without a document — a pane with no tab — is not a failure.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EditorAppearanceTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TakingTheDocumentAway_AndGivingANewOne_ReportsNothing()
        => fixture.Run(() =>
        {
            AvaloniaTextEditor editor = new() { Document = new TextDocument("class A {}") };
            Window window = Show(editor);
            List<Exception> reported = [];
            using EditorAppearance appearance = new(editor, ThemeVariant.Dark, reported.Add);

            editor.Document = null;
            Dispatcher.UIThread.RunJobs();
            editor.Document = new TextDocument("class B {}");
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(reported);
            window.Close();
        });

    [Fact]
    public void AnEditorWithNoDocument_ReportsNothing()
        => fixture.Run(() =>
        {
            // A new editor comes with an empty document of its own.
            AvaloniaTextEditor editor = new() { Document = null };
            Window window = Show(editor);
            List<Exception> reported = [];

            using EditorAppearance appearance = new(editor, ThemeVariant.Light, reported.Add);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(reported);
            window.Close();
        });

    // In a shown window, so the palette's brushes resolve as they do in the application.
    private static Window Show(AvaloniaTextEditor editor)
    {
        Window window = new() { Content = editor, Width = 400, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
