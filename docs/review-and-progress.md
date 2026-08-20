# Review and capability progress

`review-v1` is a deterministic, local scheduling algorithm. A successful café task creates review handoffs; pronunciation practice creates bounded attempt metadata; concept progression supplies due dates. `ReviewHistorySynchronizer` converts those existing records into stable phrase, concept, pronunciation-target, and recurring-form schedules. Re-running synchronization with unchanged evidence does not duplicate an item.

The scheduler receives its clock, rating, response latency, prior schedule, and versioned configuration as inputs. `Again`, `Hard`, `Good`, and `Easy` adjust difficulty and choose a bounded interval. Response latency affects the interval only through the documented slow-response threshold. The local model never creates, grades, orders, or schedules a review.

The Review screen asks the learner to retrieve before revealing the reviewed answer. The learner's explicit rating is stored as evidence. For concept items, deterministic code combines delayed recall with any already-stored communicative success; a review cannot invent communicative success that was never demonstrated in a task.

Today chooses between due review, the café scenario, and pronunciation from the same local snapshot. Progress leads with the single implemented communicative capability—ordering at a café—and reports it as not started, practicing, or demonstrated from persisted task outcomes. Concept counts are secondary. The app has no XP, currency, rank, engagement target, or punitive daily streak.

All learner-facing prompts still require a runtime-approved content catalog. If the installed pack fails linguistic or license review, Review preserves the schedule but does not expose draft teaching content.
