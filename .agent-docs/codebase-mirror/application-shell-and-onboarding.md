# Application Shell and Onboarding

## Repository paths

- `src/Linguistics.App/App.axaml.cs`
- `src/Linguistics.App/MainWindow.axaml`
- `src/Linguistics.App/MainWindow.axaml.cs`
- `src/Linguistics.App/Features/Onboarding/`
- `src/Linguistics.App/Features/Shell/`
- `src/Linguistics.App/Features/Languages/`
- `src/Linguistics.App/Features/Settings/`
- `src/Linguistics.App/Features/Learn/`

## Responsibility

Application assembly creates the concrete local repository, profile owner, providers, recording store, and fixed-field diagnostic log. The main window restores local learner data, offers byte-preserving recovery for corrupt or unfinished stores, and routes either to onboarding or the desktop shell. Onboarding saves only at explicit completion. Languages and Settings submit edited profile candidates through the same owner. The shell owns navigation across Today, Learn, Practice, Scenario, Review, Progress, Languages, and Settings, and returns to onboarding after confirmed all-data deletion. Shell, onboarding, Today, Progress, and Settings share the Paper design-system controls. Learn presents the validated course as a map and a focused card/template player. In explicit developer mode, the shell can reveal deterministic diagnostics, a PaperStage sandbox, and a synthetic template gallery that reads no learner data and saves nothing.

## Important entry points

- `App.OnFrameworkInitializationCompleted`: dependency assembly.
- `MainWindow.LoadProfileAsync`: startup restore and onboarding/shell routing.
- `OnboardingView.CompleteAsync`: converts reviewed UI choices into a new-profile input and delegates completion to `LearnerProfileOwner`.
- `MainWindow.OnRecoveryClicked`: requires a second confirmation, moves unreadable bytes to the app-owned Recovery folder, and continues to setup without reinterpreting them.
- `ShellView.ShowSelectedPage`: assembles and presents each implemented learner destination.
- `LearnView`: renders course capacity honestly, opens authored lessons, delegates template slides through the app registry, reports deterministic practice outcomes locally, and returns to the course map without writing preview mastery.
- `TemplateGalleryView`: renders every registered template from fixed synthetic fixtures, cycles preview outcomes, and exercises text-only and effective motion settings without learner state.
- `CurriculumDiagnosticsView`: renders deterministic configuration and bounded persisted aggregates without learner utterances, transcripts, audio, prompt bodies, or paths.
- `LanguagesView.OnSaveClicked`: preserves profile identity and validates preferred-language eligibility before saving.
- `SettingsView.OnConfirmDeleteClicked`: performs the second step of exact-scope profile deletion.

## Dependencies

Avalonia built-in controls and accessibility properties, `LearnerProfileOwner`, and the concrete local repository assembled by the application.

## Consumers

The desktop application window and all current Milestone 1 user journeys.

## Invariants and trust boundaries

- Views do not construct persistence services.
- Partial onboarding is not persisted.
- No microphone permission is requested during onboarding.
- New microphone audio is not retained, and no learner-facing retention preference is exposed. The stored compatibility field remains false; Settings can still delete legacy recordings.
- Developer diagnostics, PaperStage, and template-gallery routes require `LINGUISTICS_DEVELOPER_MODE=1`; gallery fixtures are fixed and keep learner history, language content, and provider bodies out of inspection.
- Startup storage errors do not silently discard or overwrite learner data.
- Recovery requires two explicit actions and preserves the original bytes under a randomized app-owned name.
- A language edit cannot silently invalidate a preferred explanation language.
- Deletion requires an explicit second action and names recordings, diagnostics, recovery copies, and learner history while preserving content, models, and unrelated files.

## Side effects

Explicit onboarding completion creates the current schema six learner data file. Languages, Settings, lesson visits, tasks, pronunciation, and review save through the profile owner and replace the envelope atomically. A runtime approved lesson records only visit counts, card position, times, IDs, and content version. Preview lessons remain session only. Startup and successful review operations may add redacted fixed field diagnostic events. Confirmed deletion coordinates recordings, diagnostics, current and temporary learner files, and app owned recovery copies, then returns to onboarding. Navigation, partial onboarding, unsaved edits, preview template outcomes, developer-gallery interaction, and cancelled deletion have no learner persistence side effects.

## Likely blast radius

Changes can affect application startup, relaunch routing, onboarding accessibility, saved preferences, exact deletion, and every navigation destination.

## Checks

- Release build with zero warnings.
- Full unit and persistence test run.
- Real macOS onboarding, incomplete-close, save, storage inspection, relaunch, mouse, and keyboard interaction.
- Real macOS Languages/Settings edits, preferred-language rejection, delete cancellation, corrupt/unfinished-store recovery, and confirmed deletion with before/after inspection.
- Windows CI build, test, and publish; Windows real interaction remains a separate evidence requirement.

## Last reconciled

Paper design system v2 Phase 2 on 2026-08-30. Real macOS evidence covers the three template renderers in the developer gallery and the authored German preview lesson, including mouse, keyboard, replay, skip, outcome, text-only, reduced-motion, light, and dark states. The native accessibility tree exposes named controls and live outcome text; direct VoiceOver remains unverified because Accessibility trust is false. Windows native interaction remains unavailable.
