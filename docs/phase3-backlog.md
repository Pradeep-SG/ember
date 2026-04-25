# Phase 3 backlog

Items deferred from Phase 2 that wait for Phase 3 (cloud sync, push, IAP, soft launch). Seeded by WP-09; later WPs append.

## From WP-09 — Persistence v2

- **Encryption at rest for sensitive records** — habit log + shadow battles encode behavior we promised to keep private. Per data-model §12 the design is "device-derived key synced through iCloud Keychain." Phase 3 wires it; Phase 2 ships plaintext local-only.
- **Conflict resolution on cloud sync** — last-write-wins per field, using each entity's `updated_at`. We added the field on every record now (see WP-09 commit) so Phase 3 doesn't need a breaking migration.
- **Firestore document mapping** — each JSON-per-entity file becomes a doc under `/players/{player_id}/<collection>/<doc_id>`. Singleton entities (warden, resonance, realm) become single docs at fixed paths. Month-sharded folders (habit_log, shadow_battles) become subcollections.
- **Server-side migrations are forbidden** — clients run their own `Migrator` against locally cached data. Server only stores; never transforms.
- **Atomic-write fault injection test** — the EntityStoreTests plan called for a fault-injection scenario asserting on-disk integrity under a faulting writer. JsonUtility doesn't expose a hookable serializer; deferred to WP-10 hygiene where a layered serializer abstraction can land.

## Carried over from Phase 1's `phase2-backlog.md`

(Items now resolved or owned by a Phase 2 WP have been removed; what's left here is genuinely Phase 3.)

- **Real analytics service integration** (Amplitude/PostHog behind `IAnalyticsSink`). Phase 1 ships NDJSON-local; Phase 2 audits and tags `(P2)` events; Phase 3 wires the network sink.
- **Push notifications** — APNs (iOS native) for the diary-cohort recall flow.
- **IAP / RevenueCat** — App Store IAP wiring.
