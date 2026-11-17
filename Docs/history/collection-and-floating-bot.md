> Historical record preserved at the Starfall+ baseline. Current direction and verified status: [project status](../PROJECT_STATUS.md) and [decisions](../DECISIONS.md).

# Collection and floating Bot

17 November 2026. This pass keeps the existing pixel artwork and minimal menu hierarchy.

## Presentation and navigation

- Home: Play, Learn maths, Collection, Settings. All actions have stepped outlines, contrasting surfaces and a shallow shadow. Three small elemental meteors and a pale pixel halo frame the astronaut without competing with the actions.
- Collection: Unlockables, Statistics, Tools. The Tools view explains six systems; the separate Blaster pose showcase has been removed.
- Unlockables: Goals and My colors. Both use continuous vertical scrolling, with a visible drag rail. Mouse wheel, pointer drag, touch drag and optional keyboard navigation share the same list. A drag threshold and release guard prevent a scrolling gesture from also claiming a reward or purchasing a color.
- Achievement medals no longer have the small sparkle at the top right.

## Help companion

Bot floats slowly along screen-specific routes in unused margins. It rests at waypoints and pauses on hover or while a pointer/touch is held. Calmer motion pins it at a safe starting position. Its generous hit target opens the existing staged, contextual guide; the conversation pauses gameplay. The opening pointer event is consumed to avoid activating an underlying control or skipping dialogue.

The same Bot appears on menus, teaching, preparation, combat and overlays. Fixed Ask for help buttons are removed. Maths-specific Hint and Work together actions remain. Bot is a local context-aware game guide, with authored explanations; no online AI service or credentials are required.

## Progressive goals

One card represents each related activity. A completed goal becomes claimable; collecting credits advances that same card to the next target. Existing progress counts toward later targets. A fully completed track stays visible as Completed. Claims are persistent and cannot be paid twice.

| Track | Targets | Credits per stage |
| --- | --- | --- |
| Build / upgrade | First Tower, 5, 15, 30, 60 installed upgrades | 1, 1, 2, 3, 4 |
| Survive nights | 1, 5, 10, 20, 40 | 2, 2, 3, 4, 5 |
| Use Blast | 3, 10, 25, 50, 100 | 1, 2, 2, 3, 4 |
| Nights without Health damage | 3, 5, 10, 20 | 2, 2, 3, 4 |
| Stop meteors | 1,000, 2,500, 5,000, 10,000, 25,000 | 2, 2, 3, 4, 5 |
| Solve with help | 5, 15, 30, 60, 100 | 1, 1, 2, 3, 4 |
| Solve independently | 10, 25, 50, 100, 200 | 2, 2, 3, 4, 5 |
| Practice days with five completed questions | 1, 3, 7, 14, 30 | 1, 2, 2, 3, 4 |
| Pilot level | 18, 25, 40, 60, 100 | 2, 2, 3, 4, 5 |
| Power | 60, 100, 180, 300, 500 | 3, 3, 4, 5, 6 |

The build track counts currently installed ranks; the night track uses highest completed night. Practice days need not be consecutive. Existing daily practice credits remain separate. Maths difficulty, learning gates and upgrade costs were not redesigned in this pass.

## Colors and migration

Orange, Green, Purple and Gold keep their original indices and prices. Blue costs 3 credits, Pink 4, Red 5 and Teal 5. Colors recolor the astronaut, equipment energy and lights using the existing palette system. Ownership and selection persist through journal/profile saves.

Old automatically paid achievements map to their matching claimed stages without adding credits again. Old balances and owned colors are retained. The migration is versioned and idempotent, including saves with absent new fields.

## Verification

`RuneholdCollectionChecks.Run` checks sequential eligibility and claims, duplicate prevention, every stage of all ten tracks, legacy migration, journal serialization, all eight profile color indices and Bot hit rejection/activation/context selection.

Final Unity 6000.6.7f1 macOS builds succeeded for both the non-saving preview (188,455,087 bytes) and normal saved expedition (188,454,991 bytes). The final screen-control retry failed at native service startup, so it did not provide an additional visual pass after these builds.

Desktop preview review verified reward collection (3 credits from the first Tower and night goals), advancement to Make 5 upgrades and Survive 5 nights, scrolling to the bottom of both lists, buying/equipping Blue for exactly 3 credits, the simplified Tools view, Bot motion and calmer-motion positioning. Native pointer automation was unreliable; actual Bot click/touch delivery and mobile multitouch require device review. The shared hit-test/context activation path passed automated checks. These checks do not establish long-term reward balance.
