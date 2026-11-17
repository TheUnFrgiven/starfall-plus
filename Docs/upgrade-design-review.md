# Starfall+ upgrade design review

17 November 2026. Presentation has changed; the six upgrade mechanics remain for review.

Every upgrade currently costs Metal and Crystal and requires one solved question from the selected adventure topic. No resources are spent on cancel, mistakes or worked examples. After a worked example the learner must answer a fresh similar question. Installed ranks carry between nights. Difficulty is chosen in Learn maths; prices do not determine educational difficulty.

## Current mechanics and choices

| Upgrade | Actual effect | Design question | Suggested direction, not yet implemented |
| --- | --- | --- | --- |
| Tower | First four purchases can add fixed towers in four map positions. Improve Towers gives every installed tower +2 damage, a faster firing interval and +4 blast radius per rank (radius capped at 140). | Should children need separate Add / Improve choices? Both are currently useful: coverage versus strength. The old screen hid the strength numbers. | Keep one Tower card. Add until all four are built, then automatically improve; choose a tower position directly if placement becomes important. |
| Shield | Rank 1 gives 50 protection; later ranks add 20. Recharges before a night. It does not heal Health. | Do we want temporary protection to be the main survival investment? | Keep. This is the clearest resource-management tradeoff. Show expected hits absorbed using the current threat. |
| Collector | Each rank gives +1 Metal and +1 Crystal per matching pile, and +10 astronaut movement speed up to the movement cap. With two piles of each resource, a rank gives +2 Metal and +2 Crystal per complete day. | Is walking to four piles fun enough to justify an upgrade? Why is movement speed attached to income? | Keep only if there is a meaningful choice between investing now and defending tonight. Remove its hidden movement benefit. An alternative is automatic income, which would be a different mechanic. |
| Blaster | +1 damage per rank; faster firing and more piercing. Extra streams every three ranks, plus pilot-level scaling; later ranks gain splash effects. | Is this the main power fantasy, or should towers do most damage? | Keep as the astronaut's clear attack upgrade. Show one primary benefit and unlock new firing patterns at visible milestones. |
| Drones | Adds orbiting attackers (up to eight), with damage, firing speed and piercing improvements. High ranks add splash. | Does this add a new decision, or is it another version of Blaster? | Treat as a later unlock if needed; otherwise merge it into Blaster milestone rewards. |
| Repair | Continuous Health repair during combat: each rank adds at least 1 Health/second, increasing with maximum Health. | Does this erase the intended pressure from persistent Health and Shield investment? | Consider limited repairs bought between nights instead. Keep Shield distinct from healing. |

Tower damage = 2 + 2 × shared Tower rank + floor((pilot level − 1) / 4). Firing interval = max(0.09, 0.32 / (1 + 0.1 × shared rank)) seconds. Thus Improve Towers is useful; with four towers, its +2 damage affects four firing sources. The balance model is a rough planning estimate, not a measured win probability.

Prices before purchase: Metal = 3 + 2 × rank (Shield starts at 4); Crystal = 1 + floor(rank/2) for Tower, otherwise 2 + floor(rank/2). Collector's first 3 Metal / 2 Crystal purchase repays its material cost in roughly two complete days of collecting all piles, ignoring the immediate defense opportunity cost.

## Questions to settle next

1. Keep four core upgrades: Tower, Shield, Blaster and income? Drones could become Blaster rewards and Repair a limited consumable.
2. Should Tower automatically add a new tower until four exist, then improve all four?
3. Keep resource collection as active exploration, or use automatic daily income with planning maths?
4. Should maths ask about the real bill, remaining supplies, expected income or protection rather than only authorizing an unrelated purchase?
5. Keep the +1 Metal reward for independent solving, or remove it so children never feel discouraged from asking for help?

The current change puts all six upgrades on one page and shows before/after benefits. It removes the Spaceship/Astronaut category split. The design alternatives above need a choice before mechanics are removed or rebalanced. Doubling the world increases travel and meteor approach distances; long-session balance still needs playtesting.
