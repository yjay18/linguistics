# Documentation Map

## Status

Milestones 2–6 are committed and pushed through `61e60ad`; Milestone 7 review/progress is `d182749`, local-data/release hardening is `34d84fd`, and the final-design slice is `f8cde75`. Paper design system v2 Phases 1 and 2 are implemented in the current uncommitted working tree as of 2026-08-30. Phase 2 adds schema-2 lesson-template content, strict attributable validation, deterministic course projection, an app-only renderer registry, a synthetic developer gallery, and the first three templates: object spotlight, picture match, and word-order train. The bundled German lesson remains machine-validated preview content, visits do not change mastery, and missing pack assets use complete authored text-only presentations. The current local Release build, 231 automated tests, and formatter pass; two consecutive post-fix Codex and Gemini visual-QA passes are clean in light, dark, text-only, reduced-motion, gallery, and real-lesson states. macOS accessibility automation exposes the named native control tree and keyboard paths were exercised, but direct VoiceOver remains unverified because Accessibility trust is false. Windows native interaction is unavailable. The last committed CI baseline remains GitHub Actions run `32342596021` for `f8cde752309e50fe3bc3d1a7a4c490562e497c29`; the present uncommitted Phase 2 work is not a remote or release claim.

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
