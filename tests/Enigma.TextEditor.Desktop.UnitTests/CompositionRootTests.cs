using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Core.Configuration;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests;

/// <summary>
/// Guards the composition root. A missing registration is invisible until the feature that needs it
/// is used, which is exactly the kind of failure a build should catch.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class CompositionRootTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(typeof(INavigationService))]
    [InlineData(typeof(IContentDialogService))]
    [InlineData(typeof(IOverlayService))]
    [InlineData(typeof(IInfoBarService))]
    [InlineData(typeof(IFileDialogService))]
    [InlineData(typeof(IFolderDialogService))]
    [InlineData(typeof(IThemeService))]
    [InlineData(typeof(IAppShutdown))]
    [InlineData(typeof(MainWindowViewModel))]
    [InlineData(typeof(WorkspaceViewModel))]
    [InlineData(typeof(StatusBarViewModel))]
    [InlineData(typeof(IDocumentService))]
    [InlineData(typeof(ToolsViewModel))]
    [InlineData(typeof(LanguageCatalog))]
    [InlineData(typeof(ILanguagePicker))]
    [InlineData(typeof(IEncodingPicker))]
    [InlineData(typeof(IConfirmationPrompt))]
    [InlineData(typeof(IBinaryFilePrompt))]
    [InlineData(typeof(ISettingsWriter))]
    [InlineData(typeof(SettingsMenuViewModel))]
    [InlineData(typeof(IAboutDialogService))]
    [InlineData(typeof(IHelpDialogService))]
    [InlineData(typeof(PreferencesViewModel))]
    [InlineData(typeof(ShellViewModel))]
    [InlineData(typeof(FirstRunViewModel))]
    [InlineData(typeof(IFirstRunDialogService))]
    public void TheContainer_ResolvesEveryService(Type serviceType)
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            using IHost host = BuildHost(folder);

            Assert.NotNull(host.Services.GetRequiredService(serviceType));
        });
    }

    [Fact]
    public void TheMainWindow_IsComposedAndItsHostsRegister()
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            using IHost host = BuildHost(folder);
            MainWindow window = host.Services.GetRequiredService<MainWindow>();
            window.DataContext = host.Services.GetRequiredService<MainWindowViewModel>();

            App.RegisterHosts(host.Services, window);

            Assert.Equal("Enigma Text Editor", window.Title);
        });
    }

    [Fact]
    public void TheMainWindow_HasAMenuBarAContentAreaAndAStatusBar()
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            using IHost host = BuildHost(folder);
            MainWindow window = host.Services.GetRequiredService<MainWindow>();
            window.DataContext = host.Services.GetRequiredService<MainWindowViewModel>();
            window.Show();

            Menu menu = window.GetLogicalDescendants().OfType<Menu>().Single();
            MenuItem file = Assert.IsType<MenuItem>(menu.Items[0]);
            MenuItem exit = Assert.IsType<MenuItem>(file.Items[^1]);

            Assert.Equal("_File", file.Header);
            Assert.Equal("E_xit", exit.Header);
            Assert.NotNull(window.FindControl<Border>("StatusBar"));

            window.Close();
        });
    }

    [Fact]
    public void Exit_AsksTheApplicationToClose()
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            FakeAppShutdown shutdown = new();
            using IHost host = BuildHost(folder);
            MainWindowViewModel viewModel = new(
                host.Services.GetRequiredService<WorkspaceViewModel>(),
                host.Services.GetRequiredService<StatusBarViewModel>(),
                host.Services.GetRequiredService<ToolsViewModel>(),
                host.Services.GetRequiredService<SettingsMenuViewModel>(),
                host.Services.GetRequiredService<ShellViewModel>(),
                host.Services.GetRequiredService<PreferencesViewModel>(),
                host.Services.GetRequiredService<IAboutDialogService>(),
                host.Services.GetRequiredService<IHelpDialogService>(),
                host.Services.GetRequiredService<FirstRunState>(),
                host.Services.GetRequiredService<IFirstRunDialogService>(),
                shutdown);

            viewModel.ExitCommand.Execute(null);

            Assert.Equal(1, shutdown.ExitRequests);
        });
    }

    [Fact]
    public void WithoutAFirstRunState_TheContainerSaysItIsNotAFirstRun()
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            using IHost host = BuildHost(folder);

            Assert.Same(FirstRunState.Returning, host.Services.GetRequiredService<FirstRunState>());
        });
    }

    [Fact]
    public void AFirstRunState_FromTheCompositionRoot_IsTheOneRegistered()
    {
        using TempFolder folder = new();

        fixture.Run(() =>
        {
            using IHost host = BuildHost(folder, FirstRunState.First);

            Assert.Same(FirstRunState.First, host.Services.GetRequiredService<FirstRunState>());
        });
    }

    private static IHost BuildHost(TempFolder folder, FirstRunState? firstRun = null)
    {
        // Never the user's own settings file, and never their log.
        ConfigLoadResult<AppSettings> settings = SettingsLoader.Load(folder.PathOf(AppPaths.SettingsFileName));

        return App.BuildHost(Application.Current!, settings, folder.PathOf("app.log"), firstRun);
    }
}
