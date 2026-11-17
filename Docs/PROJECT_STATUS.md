# Starfall+ baseline status

Updated 17 November 2026. This is the current status record; dated reports in `history/` are historical evidence, not current deployment claims.

## Included

Unity/C# pixel-art game; direct movement, auto shooting, expanding night swarms; expanded bounded ship; smaller map-relative signs; floating contextual Bot; unified six-upgrade screen; persistent Health and recharging Shield; original generated music; minimalist menus, Collection, staged rewards and colors.

Fourteen maths topics, correct Make ten/Make twenty/Make zero targets, numerical examples, difficulty selection, animated worked examples followed by a new similar problem, independent/supported evidence, accuracy/help/time/recall statistics. No Quick practice or separate astronaut-tools category remains.

Supabase account/learner/classroom/assignment/attempt/challenge storage and authorization; adult-managed username/email/password accounts and learner nicknames; private parent/teacher dashboard and opt-in class comparisons. Real signup, confirmation-email delivery and the authenticated parent dashboard were verified after the owner replaced the Gmail app password. No credentials or learner records are included in this repository.

## Still open

1. Configure Google OAuth and register/test native callbacks on exported platforms.
2. Run a regular saved build through real answers → upload → parent dashboard, plus teacher assignments/access/revocation and multiple-account comparisons. Preview intentionally does not upload gameplay events.
3. Verify password recovery, account credential edits and nickname edits end to end.
4. Deploy the committed clearer gateway email-error mapping. The previous browser deployment attempt failed; the original working account service remains deployed.
5. Durable offline attempt queues and full cross-device ship/cosmetic restore. Current retries last only for the session; ship/cosmetic saves are device-local.
6. Decide upgrade simplifications and maths tied to actual resource budgets. Existing six mechanics remain unchanged.
7. Install Android/iOS Unity modules, export, register callbacks and test real-device touch, layout, lifecycle and performance.
8. Sustained night/economy balance, full asset/animation/readability review and playtesting with the target age group.

## Baseline validation

The predecessor passed 21,000 generated-question checks, upgrade purchase guards, expanded-map collision checks, PostgreSQL isolation/replay tests, Deno checks and both Mac builds. Real confirmation and a private dashboard session were observed. These do not establish every mobile or multi-account path.

The renamed base adds checks for product identity, scene and non-destructive legacy-save copying; final results are recorded in the checkpoint/release notes. Older broad economy checks contain stale balance expectations and must be reconciled with the current design before treating them as release gates.

## Compatibility

Save filenames/schema versions, account IDs, database schema and the existing hosted dashboard URL are stable contracts. They are not rewritten to rename the product. Desktop legacy game JSON files copy once into the new app directory without replacing existing progress or deleting originals. Old browser/Godot saves and OS-level legacy preference/high-score storage are not imported. In-memory account sessions require signing in again.

The actual public client configuration is ignored. A fresh clone uses the example template. SMTP/Google secrets belong only in the hosting control plane. Google and mobile release remain pending; this is a checkpointed development base.

## Rebrand verification

The Starfall+ identity and save-migration checks, 21,000-question checks, PostgreSQL/backend tests, Deno check and both renamed Mac builds passed. The dashboard returns HTTP 200 with its Starfall+ title on both new and legacy domains. Supabase project/sender labels and Auth redirect settings use the new brand; both gateway origins are supported. Native/mobile callback completion is still pending.
