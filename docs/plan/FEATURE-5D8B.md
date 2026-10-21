# FEATURE-5D8B — About dialog like GitClient

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5d8b-about-dialog`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

The About dialog looks exactly like Enigma.GitClient's, without the "About" title at its top left.

## Context & constraints

- Here: `AboutDialogService` sets `dialog.Title = "About"`, `Content = new AboutView`, `Close` and no
  default button. `AboutView` (width 360, spacing 4) shows the icon, name, `Version x`,
  `Build sha` (selectable, whole line) and the copyright.
- Enigma.GitClient's `Services/AboutDialogService.cs` and `Views/Dialogs/AboutView.axaml`:
  - `Title = null`, `DefaultButton = DefaultButton.Close`;
  - the view: width 380, outer spacing 14;
  - the icon is a 96 px `app-256.png`;
  - an inner stack (spacing 3) holds the name (18 SemiBold, primary foreground), then "Version" and
    the version as two secondary texts;
  - then "Build" and a selectable SHA, both 11 px tertiary, the line absent without a SHA;
  - then the copyright, 11 px secondary, wrapping, absent when empty.
- `AboutViewModel` here exposes `DisplayName`, `Version`, `BuildSha`, `HasBuild`, `Copyright` from
  `AppInfo`.

## Decisions

| Decision | Rationale |
|---|---|
| Port GitClient's view markup and styles as they are; keep this app's view model, adding `HasCopyright` | "The same exact style" |
| `Title = null`, `DefaultButton = Close` in the service | No "About" at the top left; same as GitClient |
| Only the SHA is selectable, not its label | GitClient's choice: what is copied is the revision alone |

## Steps

1. `Views/AboutView.axaml`: GitClient's layout and styles; element names kept where the tests use them.
2. `ViewModels/AboutViewModel.cs`: `HasCopyright`.
3. `Services/AboutDialogService.cs`: no title; Close as default button; the comment explains why.
4. Tests (existing About tests adapted, plus):
   - the dialog has no title;
   - the version and build lines show their values, and the build line is absent without a SHA;
   - the copyright shows.

## Acceptance criteria

- Help → About shows GitClient's layout, with no title.
- Zero warnings, whole suite green.

## Out of scope

- Sharing the view between the two apps (no shared package).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How close to GitClient | The same markup and styles | The draft says "the same exact style" | Only removing the title |
| View model base | Keep this app's plain class | GitClient's `ViewModelBase` does not exist here; the shape is the same | Introducing a base class |
