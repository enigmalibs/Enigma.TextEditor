using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The editor's colouring: an editor without a document — a pane with no tab — is not a failure, and
/// links are painted in a colour of the variant's own that reads on its background.
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Links_ArePaintedInTheVariantsLinkColour_ReadableOnTheBackground(bool dark)
        => fixture.Run(() =>
        {
            ThemeVariant variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            AvaloniaTextEditor editor = new() { Document = new TextDocument("see https://example.com") };
            Window window = Show(editor);

            using EditorAppearance appearance = new(editor, variant, _ => { });

            ISolidColorBrush link = Assert.IsAssignableFrom<ISolidColorBrush>(editor.TextArea.TextView.LinkTextForegroundBrush);
            Assert.Same(Resource(editor, "EditorLinkBrush", variant), link);
            Assert.NotEqual(Colors.Blue, link.Color);

            // WCAG AA for body text: AvaloniaEdit's own blue is 1.9:1 on the dark background.
            Color background = Assert.IsType<Color>(Resource(editor, "EnigmaBackgroundColor", variant));
            Assert.True(Contrast(link.Color, background) >= 4.5, $"{link.Color} on {background}: {Contrast(link.Color, background):0.00}:1");
            window.Close();
        });

    [Fact]
    public void ASwitchOfVariant_RepaintsTheLinks()
        => fixture.Run(() =>
        {
            AvaloniaTextEditor editor = new() { Document = new TextDocument("see https://example.com") };
            Window window = Show(editor);
            using EditorAppearance appearance = new(editor, ThemeVariant.Light, _ => { });

            appearance.Apply(ThemeVariant.Dark);

            Assert.Same(Resource(editor, "EditorLinkBrush", ThemeVariant.Dark), editor.TextArea.TextView.LinkTextForegroundBrush);
            Assert.NotSame(Resource(editor, "EditorLinkBrush", ThemeVariant.Light), editor.TextArea.TextView.LinkTextForegroundBrush);
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

    private static object? Resource(AvaloniaTextEditor editor, string key, ThemeVariant variant)
        => editor.TryFindResource(key, variant, out object? value) ? value : null;

    // The WCAG 2 contrast ratio, from each colour's relative luminance.
    private static double Contrast(Color first, Color second)
    {
        double a = Luminance(first);
        double b = Luminance(second);

        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(Color colour)
        => (0.2126 * Linear(colour.R)) + (0.7152 * Linear(colour.G)) + (0.0722 * Linear(colour.B));

    private static double Linear(byte channel)
    {
        double value = channel / 255.0;

        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
