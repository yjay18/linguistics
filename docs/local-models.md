# Optional local conversation models

Linguistics does not require a language model. Scripted lessons and tasks remain the authoritative, complete path when no model is selected or Ollama is unavailable.

## Network and privacy boundary

The default adapter accepts only a plain HTTP loopback endpoint and uses `http://localhost:11434/`. Remote hosts, credentials in URLs, path aliases, redirects in the app-owned client, and model names ending in `-cloud` are rejected. Learner utterances may be sent to the selected model through the local Ollama process, but they are not included in ordinary app diagnostics or sent by Linguistics to a remote service.

## Setup behavior

Settings can check the already-running local service, list installed local models, inspect capabilities and reported license text, and save an explicit selection. Linguistics does not install or start Ollama, sign into Ollama, download a model, or recommend a model automatically. Model acquisition must remain a separate informed action showing source, size, storage, capability evidence, and license terms.

## Bounded dialogue contract

`cafe-dialogue-prompt-v1` uses the closed `cafe-dialogue-schema-v1` response shape. The request contains only the current task role, goal, state, allowed next intents and states, approved vocabulary, approved NPC response strings, established scenario facts, and the current learner utterance. It excludes the learner profile, history, content-source notes, paths, and recordings.

The model may select one supplied NPC response and propose one supplied intent/state. Exact JSON, field, identifier, response, length, vocabulary, and currently allowed-transition checks run before the proposal is returned. A timeout, cancellation, obsolete request, transport failure, malformed envelope, malformed proposal, extra field, unknown ID, cloud alias, or forbidden transition returns the scripted fallback without a state mutation. Short structured requests are deliberately non-streaming.

## Current evidence and limits

Official Ollama API, structured-output, authentication, macOS, Windows, model-detail, streaming, and MIT-license documentation was rechecked on 2026-08-20. This Mac first proved the stopped-service fallback, then a later native check found Ollama `0.32.14` running with four already-installed local models. One exact-schema smoke request through the production adapter and `llama3.2:latest` (reported 3.2B, Q4_K_M) was accepted in 12.5 seconds. Nothing was downloaded, signed into, or saved as the learner's selection. This is evidence for one local run, not a general hardware recommendation; the reported Llama 3.2 community license still needs authorized review before recommendation or redistribution.
