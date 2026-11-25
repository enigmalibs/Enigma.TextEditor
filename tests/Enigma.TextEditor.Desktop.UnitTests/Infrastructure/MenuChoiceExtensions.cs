using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Picks a value of a radio group by what its item says, as a user reads the menu.
/// </summary>
internal static class MenuChoiceExtensions
{
    extension(IReadOnlyList<MenuChoice> choices)
    {
        /// <summary>
        /// The choice whose item reads so, its access-key underscore aside.
        /// </summary>
        /// <param name="header">What the item says: <c>System</c> for <c>_System</c>.</param>
        /// <returns>The choice.</returns>
        public MenuChoice Choice(string header) => choices.Single(choice => choice.Header.Replace("_", string.Empty) == header);

        /// <summary>
        /// Runs a choice's command, as a click on its item does, and waits for it.
        /// </summary>
        /// <param name="header">What the item says.</param>
        /// <returns>A task that completes once the command has run.</returns>
        public Task ChooseAsync(string header)
        {
            MenuChoice choice = choices.Choice(header);

            if (choice.Command is IAsyncRelayCommand asynchronous)
            {
                return asynchronous.ExecuteAsync(null);
            }

            choice.Command.Execute(null);

            return Task.CompletedTask;
        }
    }
}
