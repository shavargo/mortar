# Town and Vendors (Milestone 3, town-first) — Design

Date: 2026-10-17. Build-order step 5 of `2026-10-14-arpg-framework-design.md`
(item depth and town), **town-first slice**. Status: Approved.

## Goal

Finish the headless town economy loop in `Mortar.Game`: talk to smith, healer,
and sage at town markers; buy / sell / repair / identify / heal for gold;
vendor stock that regenerates on a world-tick timer; minimal item durability so
smith repair is meaningful. Uniques and deeper durability rules follow in a
later items pass. Unity vendor UI waits until the framework is complete.

## Scope

**In**

- Hybrid vendor session: Talk (adjacent) arms a vendor; Buy/Sell/Repair/
  Identify/Heal require adjacency and matching `ActiveVendor`; Leave (or
  walking out of range / leaving town) clears the session.
- Three vendors at existing markers: `smith`, `healer`, `sage`.
- Gold economy: base `value` on item defs; buy/sell/repair/identify/heal fees
  from `rules.json`.
- Stock regen every 144 000 ticks (2 hours at 20 Hz), world-wide; level-scaled
  rolls from deepest visited dungeon depth; shop goods sold **identified**.
- Minimal durability: current/max on repairable gear; weapon wear on hit;
  armor/shield wear when the player is struck; smith restores current to max.
- Save version 3 with migration from v2.

**Out (this slice)**

- Uniques; durability-at-0 disables affixes; warrior field repair (max-dur
  decay).
- Buyback into vendor stock; Wirt-style single-item gambler; gossip/quests.
- Unity shop UI / class select (demo remains warrior-only until Unity phase).
- Real-time wall-clock timers (sim uses ticks only).

## Understanding summary

- Town-first, then a separate items pass; then Unity presentation.
- Study DevilutionX for *behavior* (proximity talk, per-NPC store, stock
  fills); invent Mortar content, prices, and tick timer.
- Deterministic netstandard2.1 sim; invalid vendor commands → `CommandRejected`.

## Approach (chosen)

**`TownServices` + `VendorStock` on `World`**, same style as portal state:
commands + one system, not map actors and not engine-owned catalogs.

| Approach | Why not |
| --- | --- |
| Vendors as town Actors | Heavy spawn/save for three static services |
| Engine-owned shop UI | Breaks sim-as-source-of-truth and hash tests |

## Architecture

### World state

- `ActiveVendor`: `null` | `smith` | `healer` | `sage`
- Per-vendor `VendorState`: kind + stock list (`Item`s owned by the shop)
- `NextStockRegenTick`: when `TickCount >=` deadline, refill all stocks and
  schedule `+= VendorStockRegenTicks` (default 144 000)

### Commands

- `TalkCommand` — town + Chebyshev ≤ 1 to marker → set `ActiveVendor`
- `LeaveVendorCommand` — clear session
- `BuyCommand(stockIndex)`, `SellCommand(itemId)`, `RepairCommand(itemId)`,
  `IdentifyServiceCommand(itemId)`, `HealServiceCommand`

### Session rules

- Before each vendor action: depth 0, active vendor set, still ≤ 1 from that
  marker; else reject. Out of range or left town → clear session and emit
  `VendorSessionEnded`.

### Stock

- On create and on regen: clear stock; roll from vendor pool at item level =
  max(1, deepest visited dungeon depth); all identified; ids from `NextItemId`.
- Unsold stock discarded on regen. No buyback this slice.
- Pools (content): smith weapons/armor/shields; healer potions; sage
  scrolls/books. Slot counts and magic% in rules.

### Prices (integer gold)

- Buy = `value` (× `magicBuyMult` if magic, floored).
- Sell = buyPrice × `sellPercent` / 100 (vendor must accept that item kind).
- Repair = `value × missing / max × repairPercent / 100` (min 1 if damaged).
- Identify / heal = flat fees (heal may be 0).

### Transactions

- Buy: afford → remove stock → stow → spend. Stow fail → reject, no spend.
- Sell: take item → gold; item not added to stock.
- Repair / identify / heal: fee then effect; reject if nothing to do.

### Durability (minimal)

- Bases: `maxDurability` (0 = N/A for potions/ammo/books/scrolls).
- Spawn/shop: `current = max`.
- Player weapon successful hit → weapon −1 (floor 0).
- Player takes hit → body and shield (if any) −1 each.
- Spells/staves: no wear this slice.
- At 0: still equippable; **affixes still apply** (deferred to items pass).
- Smith: pay → `current = max`.

### Saves

- Version 3: durability, vendor stocks, `ActiveVendor`, `NextStockRegenTick`.
- v2 → v3: gear durability = max; empty stocks + schedule regen; no session.

### Tick order

Vendor stock regen runs as a world step (with other world timers), independent
of Talk. Vendor *commands* stay in the command phase.

## Decision log

| Decision | Choice | Alternatives | Why |
| --- | --- | --- | --- |
| Phase order | Town first, items later | Balanced / items-first | Close town loop before uniques |
| Interaction | Adjacent + Talk | Same cell / always-on | Matches towner proximity pattern |
| Session | Hybrid (Talk arms; actions need adjacency) | Full session / no session | Clear enter/leave for UI later |
| Stock refresh | World-tick timer, 144 000 ticks | On town enter / on Talk | Player-requested; deterministic |
| Vendor set | Framework trio | Slimmer | Full services in one slice |
| Repair vs durability | Minimal durability now | Stub repair / full durability | Repair needs current/max |
| Pricing | Base `value` + rules | Pure tables / pure formula | Tunable without per-service JSON sprawl |
| Stock fill | Level-scaled pools | Fixed kits / flat pools | Scales with progress |
| Shop ID | Sold identified | Unidentified / mixed | Shop UX; sage still for dungeon finds |
| Architecture | World vendor state | Actor NPCs / engine catalog | Fits portal-style sim |

## Open knobs (set in plan / rules defaults)

- Exact `sellPercent`, `magicBuyMult`, repair/identify/heal fees, slot counts.
- Whether deepest depth includes only entered levels (yes: `VisitedDepths` max > 0).

## Success criteria

- Headless tests cover session, transactions, regen, durability wear, save v3
  round-trip and v2 migration.
- Release build 0 warnings; `dotnet test Mortar.slnx` green.
- No Unity work in this milestone.
