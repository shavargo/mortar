# Ranged and Magic (Milestone 2) — Design

Date: 2026-10-16. Build-order step 4 of `2026-10-14-arpg-framework-design.md`.
Status: Approved (understanding locked in session; judgment calls below).

## Goal

Extend the engine-free `Mortar.Game` simulation with projectiles, bows and
ammo, the rogue and sorcerer classes, a spell system (six starter spells),
spellbooks / scrolls / charged staves, class skills, ranged-kite AI, and a
skeleton archer — all headlessly tested. Unity presentation of cast, flight,
and portals is out of this milestone.

## Scope

**In**

- Shared projectile objects (bows, bolt spells, ranged monsters).
- Bows as ranged weapons; stackable arrows consumed per shot.
- Classes: rogue (archery) and sorcerer (spells), with data-driven kits.
- Spells: Firebolt, Healing, Lightning, Fireball, Town Portal, Identify.
- Sources: learn from spellbooks, cast from scrolls (consume), cast from
  charged staves.
- Class skills: Rogue Disarm trap (stub that rejects with a clear reason until
  traps exist); Sorcerer Recharge staff (real: mana → charges).
- Monster: skeleton archer; AI kind `ranged-kite`.
- Two-way Town Portal pair per world (dungeon cell ↔ town cell).
- Magic items may drop unidentified; Identify reveals them. Affix bonuses
  apply only while identified.
- Save version 2: spells known, staff charges, projectiles, portals, ammo
  stacks, identification flag. Version 1 saves still load (migration).

**Out**

- Unity bow/cast/portal visuals, spellbook screen, hotbar UI.
- Vendors, prices, durability, uniques (milestone 3).
- Traps beyond the disarm stub; quests; multiplayer.
- Quiver equipment slot (ammo is a normal stack in belt or bag).

## Approach (chosen)

**One projectile pipeline + data-driven spells**, wired into the existing tick
loop after action advance and before death.

Alternatives considered:

| Approach | Why not |
| --- | --- |
| Instant hitscan for bows/spells | Spec calls for projectiles; no flight, no shared path with bolts |
| Separate spell-world side channel | Duplicates collision and rejection paths |

## Tick order

1. Commands (move, attack, cast, use item, class skill, stairs, …).
2. AI (wake; melee-chase or ranged-kite intents).
3. Action advance (move / melee wind-up / cast wind-up).
4. Melee hit resolution (existing).
5. **Projectile advance** — each projectile steps; on actor or wall, apply
   effect and remove.
6. Death and loot.
7. Pickup.

Invalid commands still emit `CommandRejected` and never throw.

## Projectiles

- Spawned with: id, owner id, kind (`arrow`, `firebolt`, `lightning`, …),
  damage or effect payload, current cell, facing (or target cell for
  Bresenham), ticks of life (cap).
- Each tick: move one cell along the line toward the aim (orthogonal and
  diagonal steps allowed; same corner-cut rules as movement where relevant).
- Stop on: wall, occupied actor cell (not the owner on the spawn tick), or
  range exhausted.
- On actor hit: apply the payload (damage roll, or spell effect). Friendly
  fire: player projectiles hit monsters only; monster projectiles hit the
  player only.
- Projectiles are ordered by id for determinism. Stored on the current
  `LevelState`; cleared on level change except as required by portal travel
  (player leaves; in-flight shots on the old level are dropped).

## Bows and ammo

- A weapon base may declare `ranged: true` and optional `ammo: "arrows"`.
- `Attack` with a ranged weapon equipped does not melee. It starts a short
  shoot action; on the hit frame it consumes one ammo stack unit and spawns
  an arrow projectile aimed at the target’s cell.
- Ammo is an item base with slot `ammo`, stackable (new: items may have
  `stackMax`; arrows use it). Prefer belt stacks, then inventory. If none,
  reject with "no ammo".
- Rogue starting kit: short bow + arrow stack + light armor + potions.

## Spells

### Definitions (content)

Each spell JSON: id, name, mana cost, cast ticks, hit frame, shape
(`self`, `projectile`, `area`), effect, and scaling fields (damage range
per spell level, heal amount, radius, …). Numbers are original to Mortar.

| Spell | Shape | Effect |
| --- | --- | --- |
| Firebolt | projectile | Fire damage bolt |
| Healing | self | Restore life |
| Lightning | projectile | Lightning damage bolt (can be faster / different damage) |
| Fireball | area | After a short travel or at target cell, damage in radius |
| Town Portal | self | Open/replace the world’s portal pair |
| Identify | self / targeted item | Mark one carried item identified |

Spell level starts at 1 when learned; reading another book of the same spell
raises level by 1 (cap in rules data).

### Sources

- **Spellbook** (item use): learn or level-up the listed spell; consume the book.
- **Scroll** (item use): cast once with no mana cost if the effect is legal;
  consume the scroll. Does not require knowing the spell.
- **Staff** (weapon with `spell` + `maxCharges`): cast that spell by spending a
  charge instead of mana; recharge skill restores charges.

### Commands

- `CastCommand(spellId, targetActorId?, targetCell?, itemId?)` — for learned
  spells or staff (staff inferred when the equipped weapon provides the spell).
- `UseItemCommand` / existing item use path for books and scrolls.
- `ClassSkillCommand` — rogue disarm (always rejects "no trap here" until
  traps exist); sorcerer recharge (restores charges on equipped staff for mana).

Cast starts `ActionKind.Casting`; on the hit frame the effect fires (spawn
projectile, heal, open portal, identify).

## Town Portal

- World holds at most one portal pair: dungeon `(depth, cell)` and town `cell`.
- Casting Town Portal in the dungeon (not town) sets both ends: current cell
  on this depth, and a fixed walkable town cell (content: town portal
  anchor, or stairs plaza if unset).
- Recasting replaces the pair.
- Interacting with a portal cell (command or walk-onto intent resolved like
  stairs) warps to the other end. Either direction works.
- Portal cells are not blockers; they are markers on the level.

## Identify and magic drops

- `Item` gains `Identified` (bool). Normal items are always identified.
- Magic drops default to unidentified. Name shown to systems that care is
  a generic label until identified; affix bonuses are ignored until
  identified.
- Identify (spell or scroll) sets `Identified` on one item in belt,
  inventory, or equipment (command names the item id).
- Starting kit and spellbooks/scrolls/staves are identified.

## Classes and content

- Add `content/classes/rogue.json` and `sorcerer.json` with stats, timings,
  kits, and `classSkill` id.
- Add bow, arrows, spellbooks, scrolls, staves, and the six spells under
  `content/`.
- Add `skeleton-archer` monster with `ai: "ranged-kite"`, bow-equivalent
  ranged attack (no ammo cost for monsters), and loot.
- Spawn tables include the archer on appropriate cathedral depths.

## AI: ranged-kite

- While awake and able to shoot: if the player is in sight and within preferred
  range, attack (spawn projectile); if the player is adjacent, step away to
  a free cell that increases distance when possible; if too far, close to
  preferred range.
- Preferred range and sight come from monster data.
- Pack-leader / melee-chase unchanged.

## Saves

- `WorldSnapshot.CurrentVersion = 2`.
- v2 adds: known spells (id + level), portals, in-flight projectiles,
  `identified` on stored items, staff charges on items, ammo stacks.
- v1 → v2 migration on read: empty spell list, no portals/projectiles, all
  existing items identified, staff charges = max when the base has charges.

## Testing

Headless, deterministic:

- Arrow flight hits a monster and spends ammo; wall stop; no ammo rejects.
- Firebolt / Lightning hit; Fireball radius; Healing restores; Identify flips
  a flag and enables affixes; Town Portal round-trips dungeon ↔ town.
- Scroll cast without learning; book learn + level-up; staff charge spend and
  recharge skill.
- Rogue disarm rejects clearly; skeleton archer kites and shoots.
- Save v2 round-trip hash equality; v1 migration loads.

## Decision log

| Decision | Alternatives | Why |
| --- | --- | --- |
| Simulation-first | Vertical Unity slice; spells-first | Matches build order; Unity later |
| All six starter spells | Combat-only; Firebolt+Healing | Spec starter set; TP/Identify are sim-complete without vendor UI |
| Books + scrolls + staves | Books+scrolls; kit-only | Spec sources; recharge skill needs staves |
| Both class skills | Recharge only; neither | Spec; disarm is a cheap stub |
| Skeleton archer + ranged-kite | Archer without kite; AI on existing only | Spec roster |
| Two-way portal pair | Town recall only; instant teleport | Spec Town Portal |
| Stackable arrows (no quiver) | Infinite ammo; quiver slot | Real rejection path; less equipment surface than quiver |
| Unidentified magic in m2 | Keep 1b always-identified | Otherwise Identify is a no-op |
| Save v2 + v1 migration | Break v1; defer portals in saves | Continuity for existing saves |

## Non-goals reminder

No DevilutionX or original-game formulas, data, or names. All combat and
spell numbers live in Mortar content files and are chosen for feel and
tests, not parity.
