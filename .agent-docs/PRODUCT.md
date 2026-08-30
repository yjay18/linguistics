# Product Definition

## Vision

Build a local-first desktop language-learning application for macOS and Windows that helps multilingual adults develop communicative capability using knowledge from their whole linguistic repertoire. It must work locally without a subscription, paid inference API, mandatory account, or mandatory backend.

The first content configuration is English and Hindi as known languages with German as the target language. That configuration proves the architecture; it is not a hard-coded product boundary.

## Product promise

The learner should be able to accomplish realistic tasks, receive one useful correction at the right moment, retry, and see progress stated as capability. The app should explain when English helps, when Hindi helps, when either may interfere, and when no comparison is useful.

The learner should never need to understand which local model or deterministic component produced an interaction, but the product must remain explainable in developer mode.

## Non-negotiable principles

1. **Local first:** Core learning works without internet access after required local components and content are installed.
2. **No mandatory recurring service cost:** No paid inference, hosted database, analytics, authentication, or speech service is required.
3. **Deterministic teaching authority:** C# code and validated curriculum data decide what, when, and why to teach and how progress changes.
4. **Bounded generation:** A local LLM may realize dialogue or make schema-constrained proposals, never make authoritative curriculum decisions.
5. **Multilingual repertoire:** The learner has zero or more known languages with distinct proficiency and explanation preferences.
6. **Communication before perfection:** Task success, linguistic accuracy, fluency, pronunciation, and target-concept performance remain separate measurements.
7. **Intelligibility before accent erasure:** Pronunciation work prioritizes comprehension and communicative effectiveness.
8. **Honest capability:** Never present invented pronunciation precision, mastery, provenance, model ability, or completion evidence.
9. **Privacy by default:** No account or telemetry is required; learner history remains local; microphone audio is processed transiently on-device and is never stored.
10. **Desktop experience:** Use Avalonia with established macOS and Windows interaction patterns rather than a browser or web wrapper. Use platform adapters only where shared desktop APIs are insufficient.

## Initial learner profile

The first supported repertoire is:

- Known language: English, advanced.
- Known language: Hindi, advanced.
- Target language: German, initially A0 through an intentionally small A1-oriented slice.

The domain model must also support one or many known languages, proficiency differences, reading and listening comfort, explanation consent, preferred explanation language, and other target languages supplied by future content packs.

## Primary experience

A complete learning loop is:

1. Onboarding records the target and known-language repertoire.
2. The curriculum engine deterministically selects a due or ready concept.
3. The transfer router selects an approved bridge when useful.
4. The learner enters a realistic task with visible goals and success criteria.
5. The learner communicates by text or speech.
6. Deterministic evaluation updates task state and identifies important problems.
7. A short focus-on-form intervention appears when warranted.
8. The learner retries without losing the conversational context.
9. Task success and linguistic dimensions are stored separately.
10. Progress is updated and review is scheduled deterministically.

## Navigation and core surfaces

Use a desktop sidebar with these initial destinations:

- Today
- Learn
- Scenarios
- Pronunciation
- Review
- Progress
- Languages
- Settings

The task surface shows a goal, context, explicit success criteria, conversation, microphone and text controls, replay, slower speech, translation on request, hint, repeat, and exit. It must not automatically place an English translation beneath every German utterance.

## Onboarding requirements

Onboarding asks, in order:

1. Which language the learner wants to learn.
2. Which languages they already know, allowing multiple selections.
3. Approximate proficiency for each known language.
4. Whether they are comfortable reading and hearing each language.
5. Whether each language may be used for explanations.
6. How multilingual shortcuts should work: automatic, ask first, one preferred language, or never.
7. Whether microphone use is wanted now, later, or never.
8. Whether reduced interface motion is preferred. Speech recording retention is not offered; microphone audio is not saved.

A language switch must be visible and explained, for example as a helpful similarity, structural bridge, pronunciation shortcut, false friend, or interference warning.

Use one reusable transfer-note component so the source language, note type, explanation, dismissal, and preference controls behave consistently across lessons and tasks.

## Feedback requirements

Rank feedback in this order:

1. Communication blocking.
2. Current target concept.
3. Repeated error.
4. High-value pronunciation issue affecting intelligibility.
5. Minor form issue.
6. Style refinement.

Usually show one major correction. Smaller observations may be placed in an expandable area. A successful task is not failed merely because its language was imperfect.

## Progress requirements

Lead with situations the learner can handle, such as introducing themselves, buying a café item, identifying a destination, correcting a misunderstanding, or describing a simple physical sensation.

Secondary progress may summarize concepts practicing, strong, or due. Raw lesson count, fake currency, aggressive streak pressure, and meaningless XP are not primary measures.

## Accessibility

Every approved slice must consider:

- Keyboard navigation.
- Narrator and VoiceOver semantics and focus order.
- Scalable text and usable contrast.
- A visible reduced-motion preference that removes non-essential transitions without hiding state.
- Captions and replay for speech.
- Adjustable playback speed.
- Text-only and microphone-free modes.
- Clear permission, unavailable-service, empty, loading, error, and recovery states.

Speech is important but never mandatory for accessing the curriculum.

## First MVP boundary

The MVP proves one complete vertical loop with approximately 10–20 German concepts, reviewed English-to-German and Hindi-to-German transfer mappings, three to five task templates, one fully functioning scenario, local dialogue enhancement, local TTS, local STT, progress persistence, and deterministic review.

Candidate scenarios are introducing yourself, ordering at a café, buying groceries, asking where something is, taking a train, and a basic pharmacy interaction. Implement only the content needed to prove the loop.

## Explicitly outside the first MVP

- A1 through C2 curriculum breadth.
- A Duolingo-style economy or streak system.
- Dynamic runtime generation of canonical curriculum.
- A cloud account, synchronization backend, or hosted analytics.
- Mandatory internet connectivity.
- Phoneme-level scoring without a validated local assessment model.
- Claims about tongue position, accent percentage, or native-likeness unsupported by evidence.
- Downloadable community packs, teacher authoring, embeddings, forced alignment, or semantic search unless separately approved.
- Bundling model weights or datasets before redistribution rights are verified.

## Complete course direction

The finished product targets 450 lessons for each supported target language, with an allowed published range of 400 to 500. That number describes planned capacity, not the amount of reviewed content currently available. The interface must always distinguish authored, approved, and still planned lessons.

Each reviewed concept becomes a short card based lesson made from its goal, explanation, examples, activity, and recap. The course map groups these lessons into approachable units and leads with the next useful action instead of developer records. Motion supports attention and continuity, with an equivalent reduced motion experience.

Local models may propose presentation choices, extra practice for existing concept IDs, or draft candidates for the authoring queue. They cannot publish lessons, introduce unreviewed linguistic claims, change mastery, or choose authoritative progression. New content becomes learner eligible only after deterministic validation, provenance and license checks, competent review, and approval.

## Distribution caveat

Local development and local use can avoid recurring services. Public distribution may still require current Apple signing and notarization on macOS, Windows code signing and packaging, and channel-specific fees or accounts. Verify those requirements when release planning begins; do not describe the distributed product as cost-free without defining whose costs are meant.
