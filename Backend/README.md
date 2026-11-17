# Starfall+ accounts and learning backend

Unity stays C#. Supabase provides PostgreSQL, authentication and a small TypeScript account gateway. The adult dashboard is static HTML/JavaScript, hosted separately from the database.

## Current deployment

The Starfall+ Supabase project in the Math Game organization was used. Its public/private schemas were empty before setup. Both SQL migrations were applied through its SQL Editor. The `accounts` Edge Function is deployed, with its own credential validation and a server-side rate limit; sensitive account changes validate the current authenticated user and reauthenticate for email/password changes.

Dashboard: https://starfall-plus-dashboard.vercel.app/

Live database verification: 11 game tables; RLS enabled on all; anonymous learner/statistics reads rejected; email lookup denied to player accounts and permitted only to the server gateway. The account endpoint, dashboard origin and callback configuration have also been checked. The unrelated older LVL database was not changed. The connector still points to that older account, so this project's setup was completed through the browser.

**Still required for complete provider verification:** Google OAuth client configuration and native callback registration on exported mobile/desktop targets. Gmail custom SMTP is now configured: the owner entered and saved the app password directly in Supabase. Confirm-email remains enabled. The owner completed a real signup and delivered confirmation link; the authenticated parent dashboard was observed. Google sign-in remains unconfigured and unverified. The parental setup checkbox is an adult attestation; it is not an age/identity verification system or a substitute for a consent policy.

The database was bootstrapped through Dashboard SQL, not `db push`. Reconcile the two applied migration versions with Supabase migration history before the first CLI deployment; do not blindly rerun the initial CREATE statements on this live project.

## Account model

An adult signs up with a username, parent email and password, or Google. Passwords stay in Supabase Auth. The game asks the learner "What should we call you?" and stores a nickname. Username, parent email, password and nickname can be changed in Settings > Account. Email changes require confirmation. Google-only accounts can change their username; to establish/change a password, use verified recovery.

Each account initially owns one learner. Parents can grant another verified adult read access, join a teacher's class by its unguessable code, revoke individual access and leave a class. A teacher can create classes and topic/difficulty assignments. Teacher access comes from a parent-approved membership or explicit parent link. Editable auth metadata never assigns teacher privileges or access to another learner.

Comparisons default off. A parent must explicitly enable nickname-only comparisons. All boards are private to a class, use one topic and difficulty, and reset weekly in UTC. There is no public child directory, chat, location, birthdate or child email collection.

## Stored evidence and reporting

- Device-reported learning attempts: event ID, UTC timestamp, topic, difficulty, fact key, submitted value, correctness, first check, hint/model use, demonstration, independent completion and elapsed seconds. These are useful practice reports, not trusted competitive scores.
- Game snapshots: current night, level, meteors stopped, installed upgrade count, Health, Shield, Metal and Crystals. These are device-reported context, not leaderboard evidence.
- Server-graded challenges: server-generated problem, private expected answer, one-use challenge ID, issue/answer time, topic/difficulty, correct answer and reported help. Answers are never sent with the initial challenge. Replay, expired questions and another account's challenge are rejected.
- Dashboard: first-check numerator/denominator and accuracy, help rate, demonstrations, median correct time (exclude >180 seconds), and matched topic/difficulty comparisons between the last 7 days and preceding 7 days. Trends require at least 5 first checks in each period. The weakest sufficiently sampled topic becomes a review suggestion, not a diagnosis or a mastery certificate.
- Local Statistics: the same observed first-check/help/time metrics, review-due facts and facts independently recalled on 3+ dates. Legacy completion totals remain; historical timings and correctness rates are not invented.

Class points count distinct correctly answered, unassisted facts per day, capped at 20 daily, grouped by topic/difficulty. Raw XP, survival time, wealth and speed do not determine a child's learning rank. Device-reported/offline answers never enter the competitive board. Server grading and caps prevent forged bulk score submissions and replay; they cannot prove that a person did not use external help.

## Local verification

```
cd Backend
npm ci --ignore-scripts
npm test
npx deno@2.5.6 check --config deno.json supabase/functions/accounts/index.ts
```

The tests execute real PostgreSQL via PGlite, with a minimal Supabase Auth role fixture. They apply both migrations and check row isolation, parent-approved teacher access, revocation on class exit, blocked arbitrary writes, private boards, duplicate event handling, challenge replay/ownership, expected-answer privacy, dashboard aggregation and rate-limit thresholds. This does not replace a hosted Auth/OAuth/mail test.

## Dashboard build and hosting

Set `STARFALL_SUPABASE_URL` and `STARFALL_PUBLISHABLE_KEY`, then run `npm run build:dashboard`. Host `dashboard/dist` with HTTPS. The build rejects a secret/service-role key. `npm run preview:dashboard` serves the built files locally on port 4179.

Only public configuration is embedded. In the Edge Function set `ACCOUNT_REDIRECT_URL` to the dashboard's exact HTTPS URL and `ALLOWED_ORIGINS` to its exact origin. The current deployment has both configured. Rate buckets are HMAC-hashed with a server secret; secrets and email lookup responses never reach the Unity client. Current publishable/secret runtime dictionaries are preferred, with legacy environment variables supported during migration.

Supabase shared project domains rewrite HTML responses to plain text, so the dashboard is hosted on Vercel rather than an Edge Function. The static site contains no private learner records before adult sign-in. All subsequent requests use the adult's JWT and database authorization.

## Unity configuration

Copy `Assets/Resources/StarfallOnline.example.txt` to `Assets/Resources/StarfallOnline.json` and supply the project's URL, public key, native redirect and dashboard URL. The current local build is already connected; its configuration is ignored by Git. Never put an admin/server secret in this file.

Passwords and session tokens are held in memory, not PlayerPrefs or save files. Sessions expire and require sign-in again. Learning save files are scoped to the selected account's learner; offline play has its own learning save. New signed-in learning events sync during the session with idempotent IDs. A temporary queue retries failures, but it is not a durable offline sync engine. Ship progression and cosmetic journals still belong to the device; this is not full cross-device save restoration. Preview builds do not upload learning/gameplay events.

## Google and account emails

Enable Google in Supabase Authentication > Sign In / Providers using the owner's Google Cloud OAuth client. Use the project's Supabase callback URL shown there. Configure the dashboard HTTPS redirect and `starfallplus://auth` in Supabase's redirect allow list (already done for this deployment). The dashboard uses PKCE; Unity validates its expected callback and exchanges its one-use code using an in-memory verifier. No Gmail inbox-reading or sending scopes are requested by sign-in.

Google sign-in does not supply a production mail sender. Configure custom SMTP for confirmation and recovery email, including mail addressed to Gmail recipients. Do not put a Gmail app password, SMTP password or OAuth client secret in the game, repository or chat. A parent completes confirmation before learner data can be accessed. The project's minimum password length is 12.

The native custom URL scheme must be registered with the OS in the platform export, and tested on the target device. Android/iOS export modules are not available in the current installed Unity editor. The web dashboard's callback works independently of native callback registration.

Sources: [Supabase Google Auth](https://supabase.com/docs/guides/auth/social-sign-in/auth-google), [SMTP](https://supabase.com/docs/guides/auth/auth-smtp), [RLS](https://supabase.com/docs/guides/database/postgres/row-level-security), [Edge HTML limitation](https://supabase.com/docs/guides/functions/limits), [Duolingo progress](https://blog.duolingo.com/duolingo-score/), [spaced review](https://blog.duolingo.com/spaced-repetition-for-learning/).

## Final configuration checks

The hosted Security Advisor reported 0 errors and 0 warnings. Its two informational notices identify RLS with no client policies on the private challenge and rate-window tables; those tables intentionally allow only controlled server functions. Gmail SMTP was saved by the owner with port 587 and an encrypted/hidden password. Supabase displays its personal-mail-provider deliverability warning. The current local configuration enables email account creation after the saved SMTP setup was observed; other installations enable it only when their public `emailReady` flag is true; Google availability is read from the real Auth settings so the button stays disabled until Google is configured.

A subsequent real signup reached Auth but returned 500: Gmail rejected the stored sender credential with SMTP 535 / 5.7.8. Saving a credential did not establish that it worked. The owner subsequently replaced it directly in SMTP Settings; real signup, delivered confirmation and the authenticated parent dashboard were verified. The committed gateway source maps email failures to an actionable 503 response without leaking provider details or account existence. The native form explains unmet username, email, password-length and parent-attestation requirements before submission.

## Starfall+ adoption

This backend belongs to the root Starfall+ Unity game. The existing service IDs, schema, account records and hosted URL remain stable compatibility contracts. The clearer gateway error mapping is tested and pushed but is not yet deployed; the browser editor rejected the attempted update and the original working gateway remains live. See `../Docs/PROJECT_STATUS.md` for current boundaries.

The hosted Supabase project and SMTP display name are Starfall+. Vercel project `starfall-plus-dashboard` serves the branded dashboard at `https://starfall-plus-dashboard.vercel.app/`. The earlier domain remains a supported alias. Auth site URL and redirects, the native callback allow-list and both gateway origins were updated together; preflight requests for both dashboard origins return 204 with their matching origin.
