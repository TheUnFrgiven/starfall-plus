# Starfall+ project instructions

- Starfall+ is the active game. Work in this root Unity 6000.6.7f1 / C# project. `Assets/Scenes/StarfallPlus.unity` is the canonical scene. The older Galactacians/Godot/browser sources under `archive/` are historical and do not govern the current design.
- Read `Docs/DECISIONS.md`, `Docs/PROJECT_STATUS.md` and `PROJECT_MEMORY.md` before substantial changes. Keep status records aligned with actual deployments and observed verification.
- Preserve the established pixel-art direction, simple child-friendly names, direct player movement, map-relative building signs, floating Bot guidance, persistent Health and separately recharging Shield.
- Upgrade-economy simplifications and questions about actual resource budgets remain proposals until the owner chooses. Do not silently remove or rebalance the six upgrades.
- Keep supported learning distinct from independent completion. A worked example must lead to a fresh similar question and cannot directly authorize a purchase.
- Preserve Unity `.meta` GUIDs when renaming or moving assets. Keep stable save/account identifiers unless an explicit, tested migration is included.
- Never publish local client configuration, credentials, account sessions, student records, personal saves, generated caches or dependencies. Public configuration has a template; the owner's actual configuration stays ignored.
- Check relevant Unity/backend code, run appropriate checks and builds, and review the actual UI when possible. Report desktop, mobile, fixture and real hosted verification separately.
- The canonical repository is `https://github.com/TheUnFrgiven/starfall-plus`; active development follows the checkpoint recorded in `Docs/CHECKPOINT.md`. Preserve historical branches/tags and do not rewrite pushed history.
