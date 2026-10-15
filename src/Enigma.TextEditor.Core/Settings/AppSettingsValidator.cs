using Enigma.Configuration;

namespace Enigma.TextEditor.Core.Settings;

/// <summary>
/// Validates <see cref="AppSettings"/> as it is read.
/// </summary>
/// <remarks>
/// Ranges and enum membership are declared on the properties as attributes, which the configuration
/// library checks before this validator runs; there is no cross-property rule yet, so this class only
/// exists to name the validator the loader is handed.
/// </remarks>
public sealed class AppSettingsValidator : ConfigValidator<AppSettings>;
