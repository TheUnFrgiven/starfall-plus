# Starfall+ identity compatibility

Product and repository branding are Starfall+ / starfall-plus. The following identifiers intentionally retain historical spellings to keep existing accounts and data connected:

- `starfallplus://auth` is the new native callback. The old `starfallrelay://auth` entry is retained for compatibility; both are registered in Supabase. Google configuration and OS registration on native exports remain pending.
- `starfall-plus-dashboard.vercel.app` is the new dashboard host. The old `starfall-learning-dashboard.vercel.app` remains an alias for existing account links. Both have the Starfall+ title and must remain in gateway origins/Auth redirects while older clients exist.
- `starfall-*-v1.json`: existing learning, pilot and expedition save formats.
- The previous Unity app used company Moonvale Studio and product Runehold. Only the explicit save migration refers to those names in active code; legacy folders are left intact.

Account IDs, learner records, database schema/table names and opaque hosting IDs are not brand labels and must not be rewritten. Archived Galactacians/Godot sources retain authentic names and original records. Git history is preserved.
