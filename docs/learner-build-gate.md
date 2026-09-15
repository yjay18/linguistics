# Learner build pre-release gate

Status: work in progress. Passing this gate is evidence for owner review, not release approval.

Run the automated gate with developer mode off:

```sh
env -u LINGUISTICS_DEVELOPER_MODE \
    -u LINGUISTICS_DEVELOPER_PAGE \
    ./.dotnet/dotnet test tests/Linguistics.App.Tests/Linguistics.App.Tests.csproj \
    --configuration Release \
    --filter FullyQualifiedName~LearnerBuildGateTests
```

The command must exit successfully before a learner build can be proposed for release. It tests the learner surface only; developer tools remain available when `LINGUISTICS_DEVELOPER_MODE=1`.

## Checklist

### Automated assertions

- [ ] Developer mode and requested developer page are unset for the gate process.
- [ ] The shell declares exactly the eight learner destinations plus the two reviewed developer destinations.
- [ ] With developer mode off, the shell removes Template gallery and Paper stage from the navigation collection rather than merely styling them invisible.
- [ ] Requested developer pages are conditional on developer mode.
- [ ] Learner-facing resource values and UI labels contain none of the gate's builder vocabulary: `authored`, `authoring`, `deterministic check`, `fixture`, `template gallery`, `paper stage`, `sandbox`, or `developer mode`.
- [ ] Every `ProgressBar` outside developer-only views appears in the reviewed inventory below; any new progress indicator fails the gate until its source is classified.
- [ ] Every determinate learner progress indicator is fed by learner activity rather than content-authoring totals.

## Reviewed progress-indicator inventory

| Surface | Indicator | Source | Classification |
| --- | --- | --- | --- |
| App startup | `StartupProgress` | profile/recovery operation still running | transient operation, not progress claimed |
| Onboarding | `StepProgress` | current setup step out of seven | learner setup activity |
| Today | `Today_Preparing` | learning snapshot still loading | transient operation, not progress claimed |
| Progress | `Progress_LoadingName` | learning snapshot still loading | transient operation, not progress claimed |
| Learn | `LessonProgress` | current card index out of the active lesson's card count | learner lesson activity |
| Café scenario | `Scenario_LoadingName` | scenario still loading | transient operation, not progress claimed |
| Café scenario | `Scenario_RecordingName` | learner recording still active | transient operation, not progress claimed |
| Review | `Review_LoadingName` | review snapshot still loading | transient operation, not progress claimed |
| Pronunciation | `Pronunciation_LoadingName` | practice still loading | transient operation, not progress claimed |
| Pronunciation | `Pronunciation_RecordingName` | learner recording still active | transient operation, not progress claimed |

The only determinate bars are onboarding and lesson position. Course plan size, authored lesson count, validation status, fixture count, and template coverage are not learning progress and must not feed a learner-visible progress indicator.

## Manual checks

- [ ] Minimum-height reachability — With developer mode off, resize the learner app to 900 × 620 and scroll/focus through the entire sidebar; confirm no clipped or partially visible item follows Settings.
- [ ] Light and dark rendering — In each theme, visit every learner navigation destination at 900 × 620 and confirm no hidden builder label becomes visible because of wrapping, clipping, tooltip, or empty/error state.
- [ ] English and Hindi rendering — Switch the app language between English and Hindi, revisit every learner destination, and flag any wording that describes implementation, content production, validation, fixtures, or developer controls rather than a learner action.
- [ ] Keyboard reachability — Starting at the window's first focusable element, Tab and Shift-Tab through each learner destination and confirm focus never reaches a developer/gallery control.
- [ ] Screen-reader reachability — With VoiceOver on macOS and Narrator on Windows, enumerate the navigation and page landmarks and confirm no developer/gallery name or internal-only help text is announced.
- [ ] Progress meaning — For every visible determinate bar, perform the learner action that advances it and confirm content-authoring changes alone cannot move it.

## Evidence basis

- Brain `26apj5t6`: pre-release learner sweep for builder-facing jargon, developer navigation, and authoring progress.
- Brain `wqhgx76r`: developer galleries remained in the installed navigation collection with developer mode off.
- Brain `wp16rvz4`: learner wording/search cleanup and removal of a misleading course-authoring progress bar.
- Brain `06g9etve`: partially visible developer navigation at minimum height remained an open cosmetic observation.
- Brain `qpt3r426`: prior completion evidence is assistant-reported and awaits owner verification.
- Brain `m79p6rny`: Full Assurance requires native interaction, the full suite, and multi-language/theme checks.
