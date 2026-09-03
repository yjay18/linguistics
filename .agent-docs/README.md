# Documentation Map

## Status

Milestones 2–7 and the original final-design slice are committed. Paper design system
v2 Phases 1–6 are implemented on `main` through the Hinglish extension checkpoint
`bebc7e6`. Phase 3 supplies the validated local 12-image asset pipeline and attribution
surfaces. Phase 4 contains the exact 58-template catalog, P4.T1–P4.T58, with strict
validation, deterministic outcome mapping, synthetic all-outcome fixtures, replay and
skip choreography, reduced-motion completion, keyboard names and live regions, and
complete text-only behavior. Phase 5 adds explicit English, Hindi, and Hinglish
instruction-language routing without duplicating German target content. Hinglish uses
the canonical `hi-latn` code and static authored Preview copy, never runtime
transliteration. English app chrome is the current fallback for Hinglish lessons.

Phase 6 turns Learn into a paper journey and focused template player, composes Scenario
Theatre and Consequence Verdict around the existing café controller, adopts Review Flash
without moving scheduling out of the deterministic core, and projects the existing
capability overview through Progress Shelf. Today remains capability-first. Renderers
retain transient presentation state only and report through callbacks; they never own
curriculum, mastery, scheduling, transfer selection, or persistence.

The bundled German lessons, gallery copy, and assets remain machine-validated Preview
material. Preview visits do not change mastery, and production Scenario and Review
correctly fail closed until competent content and license approval exists. Locked
restore, formatter, zero-warning Release build, all 380 automated tests, publish
inspection, and fresh native macOS interaction are clean. Native evidence covers both
themes, two window sizes, reduced motion, mouse and keyboard operation, replay, skip,
lesson outcomes, scenario failure/retry/success, review reveal/rating, and capability
selection. Hinglish-specific QA covers onboarding, preferred routing, eligibility
feedback, persistence, course projection, Languages, and Settings in both themes.
Direct VoiceOver remains unverified. Windows native work is intentionally
deferred under the current macOS-only scope. Real microphone capture, configured local
recognition, drag-specific macOS automation, and competent Hinglish review also remain
unverified. Culture Plate,
Sign Reading, café-worker, and café-interior media follow-ups retain authored text-only
equivalents. The last historical committed CI baseline remains GitHub Actions run
`32342596021`; current local and remote checks do not imply content approval,
distribution, or release.

These documents turn the founding product brief into durable implementation guidance. They describe intended behavior, not verified implementation. A statement becomes an implementation fact only when repository code and evidence demonstrate it.

The user intentionally requested this documentation to remain Git-ignored. It is therefore local to this checkout, absent from clones, and not protected by CI. Revisit that decision before involving another machine or contributor.

## How to use this set

1. Start with root `AGENTS.md`.
2. Read `PRODUCT.md` and `MILESTONES.md` before planning a milestone.
3. Read only the canonical domain documents touched by the requested slice.
4. Inspect the code and nearest tests once they exist; documentation never overrides direct code evidence without reconciliation.
5. Select Token Saver or Full Assurance using `AGENT_WORKFLOW.md`.
6. Build an acceptance envelope and evidence matrix before substantive implementation.
7. Update a canonical document only when its durable truth changes.
8. Update a codebase-mirror page only when responsibility, dependencies, consumers, invariants, impact, or checks change.

## Canonical ownership

| Document | Owns | Does not own |
| --- | --- | --- |
| `PRODUCT.md` | Product promise, users, experience, MVP boundaries, accessibility | Internal module design |
| `ARCHITECTURE.md` | Layers, protocols, data flow, state authority, persistence boundaries | Detailed pedagogy or pack schema |
| `CURRICULUM.md` | Teaching model, curriculum graph, tasks, errors, progression, review | Serialization details |
| `CONTENT_PACK_SPEC.md` | Pack layout, manifests, schemas, validation, provenance | Transfer-routing policy |
| `TRANSFER_MAPPING_SPEC.md` | Transfer data and deterministic bridge selection | General target-language concepts |
| `SPEECH.md` | Audio capture, STT, TTS, pronunciation, speech accessibility | LLM dialogue policy |
| `OLLAMA.md` | Local LLM provider, schemas, prompts, failures, diagnostics | Curriculum decisions |
| `PRIVACY.md` | Local data, transient-audio policy, deletion, permissions, telemetry policy | General release mechanics |
| `MILESTONES.md` | Seven-stage sequence, steps, scope, acceptance gates | Domain rules duplicated elsewhere |
| `EXPERIENCE_REDESIGN_PLAN.md` | Paper-theatre identity, template engine and catalog, asset pipeline, instruction-language work list | Deterministic-core rules owned elsewhere |
| `AGENT_WORKFLOW.md` | Feature execution, modes, evidence, review, authority | Product requirements |
| `QUALITY_AND_RELEASE.md` | Required gates, drift checks, commits, distribution readiness | Feature-specific acceptance behavior |
| `codebase-mirror/` | Description-only impact maps for important implemented files | Source copies or task recaps |

## Decision hierarchy

When sources disagree, use this order and reconcile the lower source:

1. The user's current explicit instruction.
2. Safety, authorization, privacy, and platform constraints.
3. Accepted product behavior and architecture decisions in this set.
4. Current repository code, tests, and configuration, with conflicts surfaced rather than hidden.
5. Historical plans, conversations, examples, and non-binding model suggestions.

## Vocabulary

- **Known language:** A language already in the learner's repertoire, including proficiency and explanation preferences.
- **Target language:** The language currently being learned.
- **Concept:** A versioned learnable unit in a target-language graph.
- **Transfer mapping:** Reviewed data relating one known language to one target concept.
- **Bridge:** A transfer mapping selected for a particular learner and concept.
- **Task:** A communicative goal governed by deterministic state and success conditions.
- **Micro-intervention:** A short, targeted correction delivered during or immediately after communication.
- **Content pack:** Versioned curriculum data separate from learner history and application code.
- **Provider:** A replaceable boundary around a local model, speech system, repository, or evaluator.
- **Evidence:** A reproducible artifact supporting an acceptance claim.

## Fact-check boundary

Named models, libraries, OS APIs, redistribution rights, signing requirements, and hardware capabilities can change. Treat examples in the founding brief as candidates. Verify current official documentation and licenses immediately before adopting, bundling, or distributing them.
