# Derelict Empires — Research UI Specification
## Galaxy Map Screen Research Surfaces
_Version 1.0 — April 2026 — Supplement to `derelict_empires_ui_spec.md`_

---

## 1. Design Goals

Research in Derelict Empires is a slow, always-running background system that must remain visible and actionable without dominating the galaxy screen. Three surfaces cooperate:

- **Topbar strip** — two persistent progress bars, one per active track. Always visible, glanceable, color-coded to what is being researched.
- **RESEARCH tab** (left panel) — full control over both active tracks, their queues, and a summary of banked module progress.
- **Tech Tree overlay** — full-screen modal for selecting and queueing research, single-color-focused.

**Design principles:**
- Two research tracks, always: `TIER` (unlocks a color × category × tier) and `MODULE` (researches one specific subsystem from an unlocked tier).
- Every module and every tier has **memory** — a persistent 0–100% value that accumulates from multiple sources (active research, salvage, tech rental, espionage). The UI must expose this memory.
- Most players will focus on 1–2 colors. The selection UI optimizes for deep drill-down into one color, not a bird's-eye view of all five.
- Zero forced micromanagement. Queueing, auto-redirect, and memory-resume all work without the player touching the RESEARCH tab every few minutes.

---

## 2. Research Model (Mechanics Recap for UI Reference)

### 2.1 Two Parallel Tracks

| Track | What it does | What the player selects |
|---|---|---|
| **TIER** | Unlocks a `(color, category, tier)` node. On completion, rolls in 2 of the 3 modules as researchable; 3rd becomes a locked slot awaiting salvage/trade. | A category's next unlocked tier, e.g. "Red Weapons T3". |
| **MODULE** | Researches one specific subsystem, e.g. "Red Rail T3", drawing from any already-unlocked tier's available modules. | One module from the pool of available modules across all unlocked tiers. |

Both run simultaneously. Each has its own queue (see §5).

### 2.2 Memory Sources

Every module and every tier has a persistent progress value. Contributions:

| Source | Fills Module? | Fills Tier? | Rate |
|---|---|---|---|
| Active TIER research | — | ✓ | Primary rate |
| Active MODULE research | ✓ | — | Primary rate |
| Salvage (matched color + tier) | ✓ (specific module) | ✓ (gap-dependent) | Lump sum on salvage complete |
| Tech rental | ✓ (rented module) | ✓ (small) | Continuous while rented, diminishing over time |
| Espionage (Steal Research) | ✓ (targeted module) | — | Lump sum on operation success |
| Empty-mod redirect | — | ✓ (50% bonus) | Whenever MODULE track is idle |

### 2.3 Gating

- **Tier sequential within category**: must complete Red Weapons T1 before starting T2.
- **2-of-3 roll happens on tier completion**, not up front. Before tier unlock, all three modules are silhouetted. After unlock, 2 are `[AVAILABLE]` and 1 is `[LOCKED — SALVAGE / TRADE]`.
- **Cross-color research** has no cost multiplier — same base cost, slower progress via lower color expertise. Displayed as `Speed: 0.4×` not as an inflated cost.

---

## 3. Topbar Integration

### 3.1 Structural Change

The current topbar credits block (~120px wide, olive-green tint) expands to **~220px** to absorb two progress rows beneath the credits row. Total topbar height remains **68px**. The five faction resource boxes shrink proportionally (they are `flex: 1` each and will recompute).

```
┌────────────────── Credits + Research Block (220px) ──────────────────┐
│  ⏺  500            +2/cycle                                          │   ← credits row (22px)
│  ────────────────────────────────────────────────────                 │   ← 1px divider rgba(255,255,255,0.06)
│  TIER  ▬▬▬▬▬▬░░░░░░░░░░░   RED WEAPONS T2         42% ~3:12          │   ← tier row (20px)
│  ────────────────────────────────────────────────────                 │   ← 1px divider
│  MOD   ▬▬▬▬▬▬▬▬▬▬▬▬▬░░░   RED PLASMA T1          88% ~0:45          │   ← mod row (20px)
└───────────────────────────────────────────────────────────────────────┘
                                                                         68px total
```

### 3.2 Credits Row (unchanged from existing spec)

See §6 of `derelict_empires_ui_spec.md`. Height reduced from the previous implicit 68px to explicit **22px** to make room for the two bar rows.

### 3.3 Research Row Spec (per row)

Each row is **20px tall, 220px wide, with the following horizontal layout:**

| Zone | Width | Content |
|---|---|---|
| Label | 32px | `TIER` or `MOD` — Share Tech Mono 8px ALL-CAPS, letter-spacing 1.5px, color `TextFaint` (`#354e62`) |
| Progress bar | 78px | See §3.4 |
| Project name | 76px (flex) | Truncated with ellipsis. Exo 2 9px weight-500 ALL-CAPS, color = faction glow color |
| Value | 34px | See §3.5 |

Padding: `3px 8px`. Row is one clickable unit (see §3.6).

### 3.4 Progress Bar Spec

- Height: **3px** (visually distinct from the 2px bars in POI list — research bars are slightly heavier since they're always-visible)
- Track: fill `rgba(20, 30, 48, 0.9)`, 1px outer border in faction **base** color at 40% opacity
- Fill: faction **glow** color at 85% opacity, left-aligned, width = progress %
- **Near-completion pulse** (>90%): fill alpha oscillates between 75% and 100% on a 0.5s sine cycle. No size change, no position change. This is the only animation in the research surfaces.
- **Banked-progress tick** (tier bar only): a faint vertical line at the % of progress that came from memory sources (salvage/rental) before active research began. Color: faction base at 60%, 1px wide. Communicates "you didn't start from zero."

### 3.5 Value Display

Right-aligned, `Share Tech Mono 9px`, faction glow color.

**Toggle modes (click the value to cycle):**
- `42%` — percentage complete
- `~3:12` — estimated time remaining at current research rate
- `+0.8/s` — current research rate (in RP/second)

The mode preference persists per track, stored in user settings. Default: percentage on first launch.

### 3.6 Row Click Behavior

- **Left-click row**: opens RESEARCH tab in left panel if closed, scrolls to and highlights the matching active block.
- **Right-click row**: opens Tech Tree overlay (§5) scoped to the active project's color.
- **Hover tooltip**: shows detailed breakdown — rate, sources contributing, ETA, and `[CHANGE] [PAUSE]` quick actions. Tooltip uses standard glass material with top accent line.

### 3.7 Idle States

**TIER track idle** (no active tier):
- Bar: empty, no fill
- Label: `TIER`
- Project name: `IDLE — SELECT`, color `TextDim` (`#5a7a8e`)
- Value: hidden
- Row background: faintly pulses in `AccentDim` (`rgba(34,136,238,0.18)`) every 4 seconds as a subtle "you have nothing queued" nudge. Max 3 pulses then stops until player interacts with anything else.

**MODULE track idle** (no active module):
- Bar: empty
- Label: `MOD`
- Project name: `IDLE · +50% → TIER`, color `#66dd88` (positive delta color — the redirect is a *good* thing)
- Value: hidden
- No pulse (the redirect makes idle non-problematic).

**Both idle** (brand new game or everything complete):
- Both rows as above. TIER row gets the pulse; MOD row does not (MOD needs TIER to unlock modules first).

### 3.8 Completion Toast

When a track completes:
- Bar fills to 100%, holds for 0.8s at full brightness.
- Row background briefly flashes with faction glow color at 30% opacity (1.2s ease-out).
- Recent Events widget (bottom-right) receives entry: `● Research completed: Red Weapons T2 · 2 modules unlocked` with faction-colored dot.
- If queue has next entry: track auto-advances, bar resets to 0% (or to banked memory if any), name changes. No modal interrupt.
- If queue is empty: row goes to idle state (§3.7).

---

## 4. RESEARCH Tab (Left Panel)

Replaces the fleet list when the RESEARCH tab is active in the 4-tab bar. Panel width remains **270px**.

### 4.1 Structure

```
┌──────────────────────────────────┐
│ FLEETS · COLONIES · RESEARCH · BUILD │  ← tab bar (unchanged)
├──────────────────────────────────┤
│                                    │
│  SECTION 1: ACTIVE TIER           │  (88px)
│  SECTION 2: ACTIVE MODULE         │  (88px)
│  SECTION 3: QUEUES                │  (collapsible, ~130px open)
│  SECTION 4: BANKED PROGRESS       │  (fills rest, scrollable)
│                                    │
└──────────────────────────────────┘
```

### 4.2 Section 1 & 2 — Active Track Blocks

Shared layout for both ACTIVE TIER and ACTIVE MODULE. 88px tall each.

```
┌──────────────────────────────────┐
│ ▮ ACTIVE · TIER                    │  ← 3px left accent bar (faction glow)
│                                    │
│ RED WEAPONS T2                     │  ← Exo 2 13px weight-600 ALL-CAPS, faction glow
│                                    │
│ ▬▬▬▬▬▬░░░░░░░░░░░ 42% · ~3:12    │  ← 4px tall bar (thicker than topbar)
│                                    │
│ Red expertise 1.4× · +0.8/s        │  ← Barlow Condensed 10px, TextBody
│                                    │
│ [ CHANGE ]    [ PAUSE ]   [≡ QUEUE]│  ← 3 buttons, each 28px tall
└──────────────────────────────────┘
```

**Spec details:**
- Left accent bar: 3px wide, full height of section, faction **glow** color at 80% opacity. If track is idle: `TextFaint` instead.
- Section header: `Share Tech Mono 8px` ALL-CAPS letter-spacing 1.5px, `TextDim`. Text: `ACTIVE · TIER` or `ACTIVE · MODULE`.
- Project name: faction glow color; if off-color from current empire focus, append a small 9px color pip before the name.
- Progress bar (inner): same fill rules as topbar but **4px** tall for legibility in panel context.
- Info line: rate, expertise multiplier, and a **sources** string if memory contributed. Example: `Red expertise 1.4× · 34% banked from salvage · +0.8/s`.
- Action row: three buttons, each `(width-6)/3` wide.
  - `[CHANGE]`: opens Tech Tree overlay (§5), scoped to track type and current color.
  - `[PAUSE]`: toggles active → paused. Paused state: bar fill desaturated to 35%, value text shows `PAUSED`. No research gain while paused. Salvage/rental memory still accrues.
  - `[≡ QUEUE]`: expands/collapses the matching queue row in Section 3. Badge on button when queue has entries: small faction-glow circle with count, e.g. `[≡ QUEUE ②]`.

**Idle state block:**
```
┌──────────────────────────────────┐
│ ▮ IDLE · TIER                      │  ← accent bar: TextFaint
│                                    │
│ NO PROJECT SELECTED                │  ← TextDim
│                                    │
│ ────────────────────────────────   │  ← empty bar, no fill
│                                    │
│ Select a tier to begin research.   │  ← Barlow Condensed 10px, TextDim
│                                    │
│ [  SELECT TIER  ]  (full-width)    │  ← primary button, Accent styling
└──────────────────────────────────┘
```

### 4.3 Section 3 — Queues

Collapsible. Collapsed by default; expands when either `[≡ QUEUE]` button is tapped. Shows **two independent queues** — TIER queue and MODULE queue.

```
┌──────────────────────────────────┐
│ ▼ QUEUES                           │  ← header, Share Tech Mono 8px
│                                    │
│ TIER QUEUE (2)                     │  ← faction-neutral label
│  1. ▮ Red Weapons T3      [✕]     │
│  2. ▮ Blue Sensors T2     [✕]     │
│  [ + ADD TIER ]                    │
│                                    │
│ MODULE QUEUE (1)                   │
│  1. ▮ Red Rail T2 (88% banked) [✕]│
│  [ + ADD MODULE ]                  │
└──────────────────────────────────┘
```

**Queue entry row spec:**
- Height: 32px
- Left accent bar: 2px wide, faction glow of that entry's color
- Index number: `Share Tech Mono 9px`, `TextDim`
- Name: `Exo 2 10px` weight-500, color `TextLabel`
- Banked progress hint (mod queue only): `Share Tech Mono 8px` `TextDim` in parentheses
- `[✕]`: 20×20px, removes from queue. Hover: border `rgba(255,80,80,0.4)`, icon color `#ff6655`.
- Drag-and-drop to reorder (desktop) — 6px grab handle on left of row, cursor changes to grab on hover.

**Queue rules:**
- Each queue holds up to **5 entries**. Hard cap — keeps the player from setting it and forgetting for hours of gameplay.
- `[+ ADD TIER]` and `[+ ADD MODULE]` buttons open the Tech Tree overlay scoped to their track type, with a "queueing" indicator in the overlay header (see §5.8).
- When active track completes, queue[0] auto-starts, rest shift up. Recent Events records: `● Research queued: Blue Sensors T2 started`.

### 4.4 Section 4 — Banked Progress

Scrollable list showing every module with non-zero memory, grouped by color. Lets the player audit their banked-but-not-researched progress at a glance.

```
┌──────────────────────────────────┐
│ BANKED PROGRESS                    │  ← Share Tech Mono 8px header
│ ─────────────                      │
│  ▮ RED                             │  ← color subheader, Exo 2 11px
│   Red Rail T2        ▬▬▬ 34% [salv]│
│   Red Reactor T2     ▬░░  8% [trade]│
│                                    │
│  ▮ BLUE                            │
│   Blue Pulse T3      ▬▬▬▬▬ 62% [spy]│
│                                    │
│  [ SHOW ALL MODULES ▾ ]            │  ← bottom toggle
└──────────────────────────────────┘
```

**Row spec:**
- Height: 28px
- Module name: `Exo 2 10px`, `TextBody`
- Inline bar: 40px wide × 2px tall, same fill rules as topbar
- Percentage: `Share Tech Mono 9px`, faction glow color
- Source tag: `Share Tech Mono 7px` letter-spacing 1px, in brackets, `TextDim`. Values: `[salv]`, `[trade]`, `[spy]`, `[mix]` if multiple.

**Interaction:**
- Click a row to open Tech Tree overlay focused on that module (pre-selects it in the module focus panel).
- `[SHOW ALL MODULES]` toggle expands to include all unlocked modules (including 0% banked) for broader planning.
- Only shows modules whose tier has been unlocked. Silhouetted/locked modules don't appear here — they live in the Tech Tree overlay.

---

## 5. Tech Tree Overlay

Full-screen modal over the galaxy map. Glass material consistent with other panels. Opened via:
- `[CHANGE]` button on either active track block
- `[+ ADD TIER]` or `[+ ADD MODULE]` from queue section
- Banked Progress row click
- Hotkey `T`
- Topbar row right-click

### 5.1 Layout

```
┌────────────────────────────────────────────────────────────────────┐
│  TECH TREE                                                   [ ✕ ] │  ← 48px header
│  ──────────────────────────────────────────────────────────────── │
│  [ RED ]  [ BLUE ]  [ GREEN ]  [ GOLD ]  [ PURPLE ]               │  ← 44px color tabs
│  ──────────────────────────────────────────────────────────────── │
│                                                                    │
│  CRIMSON FORGE — Tier matrix                                       │
│                                                                    │
│   ┌──────────────────────────────┐  ┌───────────────────────────┐│
│   │ CATEGORY MATRIX (5×5)        │  │ FOCUS PANEL               ││
│   │                              │  │                           ││
│   │              T1  T2  T3  T4  │  │ RED WEAPONS T2            ││
│   │ WEAPONS      [✓][▶][○][·]    │  │ ────────────────          ││
│   │ SENSORS      [✓][○][·][·]    │  │ [Description text]        ││
│   │ INDUSTRY     [○][·][·][·]    │  │                           ││
│   │ LOGISTICS    [○][·][·][·]    │  │ MODULES (2 of 3)          ││
│   │ SPECIAL      [✓][○][·][·]    │  │  ● Red Plasma T2  AVAIL   ││
│   │                              │  │  ● Red Rail T1    AVAIL   ││
│   │ Expertise:   ▬▬▬▬▬▬ 1.4×     │  │  ○ Red Armor T1   LOCKED  ││
│   └──────────────────────────────┘  │    (salvage or trade)     ││
│                                      │                           ││
│                                      │ [ START TIER RESEARCH ]   ││
│                                      └───────────────────────────┘│
└────────────────────────────────────────────────────────────────────┘
```

**Dimensions:**
- Header: 48px
- Color tabs: 44px
- Main area fills rest. Left section (matrix): 60% width. Right section (focus panel): 40% width.
- Inner padding: 24px on all sides.

### 5.2 Color Tabs

5 tabs, equal width across the header row.

- Tab size: `(width-48)/5` × 44px, with 48px total for left and right margins
- Font: `Exo 2 13px` weight-500 ALL-CAPS, letter-spacing 2px
- Inactive tab: color = faction **base** at 60% opacity, no background
- Hover: color = faction glow, background `rgba(faction-glow, 0.08)`
- Active: color = faction glow, background `rgba(faction-glow, 0.14)`, bottom border `2px solid faction-glow`
- Active tab also displays a faint 1px glow in the faction's color around its text

**Persistence:** Last-selected color persists across overlay open/close. On first game launch, defaults to player empire's primary color.

### 5.3 Tier Matrix

**Grid structure:** 5 rows (categories) × N columns (tiers, currently T1–T5 with columns added as design expands).

**Cell size:** 80px × 80px with 6px gap between cells.

**Tier node states:**

| State | Symbol | Visual |
|---|---|---|
| Completed | `✓` | Filled faction glow at 90%, white checkmark centered. Module list underneath shows which 2 of 3 unlocked. |
| Active (researching) | `▶` | Hollow faction glow border 2px, inner fill at 30%, pulsing border at 0.5s cycle. Shows live % at bottom of cell. |
| Available (predecessor done) | `○` | Hollow faction glow border 1.5px, transparent fill. Banked progress shown as thin fill at bottom. |
| Locked | `·` | Dim faction base color at 25%, 1px border at 15%. No interaction except tooltip. |
| Queued | `⧗` | Hollow faction glow dashed border (dash 3 3), small queue-position number in corner. |

**Cell content (all states):**
- Top: Tier label (`T1`, `T2`, etc.) — `Share Tech Mono 11px`, faction glow
- Middle: State symbol, 24px, centered
- Bottom: Progress % if active, banked % if available-with-memory, else blank — `Share Tech Mono 8px`, faction glow at 60%

**Row labels:** Category names on the far left, vertical alignment middle of row.
- Font: `Exo 2 11px` weight-500 ALL-CAPS letter-spacing 1.5px
- Color: `TextLabel` (`#b8d2de`)
- Width: 80px, right-aligned

**Column labels:** Tier numbers across the top of the matrix.
- Font: `Share Tech Mono 9px` letter-spacing 2px
- Color: `TextDim`
- Height: 20px, centered

### 5.4 Expertise Bar (below matrix)

A single-row summary of the player's color expertise for the currently selected color. Width matches matrix width.

```
Expertise:  ▬▬▬▬▬▬▬▬▬░░░░░░  1.4×   (research +40% · salvage +22%)
```

- Bar height: 4px
- Fill: faction glow at 85%
- Multiplier: `Share Tech Mono 14px`, faction glow color
- Breakdown in parentheses: `Barlow Condensed 10px`, `TextDim`

### 5.5 Focus Panel

Right side of overlay. Shows detail for whatever is selected in the matrix. Three possible states:

**(A) Tier node selected (any state):**

```
RED WEAPONS T2
─────────────────────────────────────
Broadening. Second weapon or defense
type arrives. Color differentiation
visible.

PROGRESS
  Active: 42% · ~3:12 · +0.8/s
  Banked: 34% (salvage, rental)

MODULES (2 of 3 unlocked at completion)
  ● Red Plasma T2        [AVAILABLE]
    +38 dmg · armor degrade
  ● Red Rail T1          [AVAILABLE]
    Kinetic, highest damage T1 rail
  ○ Red Armor T1         [LOCKED]
    Unlock: salvage Red T2 derelict,
    trade from another empire, or
    Creative trait reroll

[  START TIER RESEARCH  ]  ← primary
[  + QUEUE  ]              ← secondary
```

**(B) Module node selected (unlocked tier, available module):**

```
RED PLASMA T2
─────────────────────────────────────
Upgraded plasma weapon. Higher per-
hit damage, armor degrade.

STATS
  Damage         38/hit
  Armor degrade  +5/hit
  Rate of fire   0.8s
  Range          700
  Supply cost    medium

PROGRESS
  Current: 12% (from tech rental)

RESEARCH RATE (at current expertise)
  Active: ~+1.2/s · Est. 1:22 from now

[  START MODULE RESEARCH  ]
[  + QUEUE  ]
```

**(C) Locked module selected:**

```
RED ARMOR T1
─────────────────────────────────────
Red's signature defense. Highest flat
HP, highest damage reduction per hit.
Expensive parts to repair.

STATUS: LOCKED SLOT
This module was not rolled when Red
Weapons T2 was unlocked. Obtain via:

  · Salvage a Red T2 derelict
  · Trade for the module with another
    empire that has researched it
  · Spend a Creative trait reroll
    (available: 1)

[  SPEND CREATIVE REROLL  ]   (if available)
```

### 5.6 Focus Panel Typography

- Title: `Exo 2 16px` weight-600 ALL-CAPS letter-spacing 1.5px, faction glow color, optional glow filter
- Divider: 1px line `rgba(60,110,160,0.30)`, 12px vertical margin
- Description: `Barlow Condensed 12px` weight-400, `TextBody`
- Section headers (PROGRESS, MODULES, STATS): `Share Tech Mono 9px` ALL-CAPS letter-spacing 2px, `TextDim`
- Stat rows: label `Barlow Condensed 11px` `TextDim`, value `Share Tech Mono 11px` `TextLabel`
- Module item dots: 8px circle, faction glow color (filled for AVAILABLE, hollow for LOCKED)

### 5.7 Primary Action Button

Width 100% of focus panel. Height 44px. Label `Share Tech Mono 10px` ALL-CAPS letter-spacing 2px.

- Default: fill `rgba(34,136,238,0.18)`, border `1px solid rgba(34,136,238,0.45)`, color `#55bbff`
- Hover: fill `rgba(34,136,238,0.28)`, border `Accent`, inner glow `rgba(34,136,238,0.15)`
- Disabled (tier gated, already researching, etc.): fill `rgba(16,24,36,0.6)`, border `BorderDim`, color `TextFaint`, with reason tooltip on hover

Secondary `[+ QUEUE]` button: 44px height, 40% width inline with primary. Styling as default action button from existing UI spec.

### 5.8 Queue-Mode Header Variant

When overlay is opened via a `[+ ADD TIER]` or `[+ ADD MODULE]` button, the header gains a sub-label:

```
TECH TREE                              [ ✕ ]
QUEUEING: MODULE TRACK
```

- Sub-label: `Share Tech Mono 9px` ALL-CAPS letter-spacing 2px, color `#55bbff`
- Selectable nodes are filtered to match the queue type. Non-matching nodes (e.g. tier nodes when queueing a module) are dimmed to 35% and non-interactive.
- Primary action button changes label: `START TIER RESEARCH` → `QUEUE THIS TIER`.

### 5.9 Overlay Interaction Rules

- Clicking empty space closes the overlay.
- `Esc` key closes the overlay.
- Opening the overlay pauses neither speed nor time — the game continues in real-time behind the glass. This is consistent with the "no modal-breaks-flow" principle.
- Starting research from the overlay auto-closes it and snaps focus to the matching active block in the RESEARCH tab.

---

## 6. Interaction Flows

### 6.1 First Research Selection (new game)

1. Player starts game. Topbar shows both tracks idle; TIER row pulses.
2. Player clicks TIER row → RESEARCH tab opens, ACTIVE TIER shows `[SELECT TIER]` primary button.
3. Clicking `[SELECT TIER]` opens Tech Tree overlay on player's starting color.
4. Player clicks a T1 tier node (e.g. Red Weapons T1) → focus panel populates.
5. Player clicks `[START TIER RESEARCH]` → overlay closes, topbar TIER bar begins filling, RESEARCH tab's ACTIVE TIER block populates.

### 6.2 Starting a Module After First Tier Completes

1. TIER completes — Recent Events toasts "Red Weapons T1 complete · 2 modules unlocked".
2. MODULE track, if idle, remains idle (does not auto-select). Player chooses what module to research.
3. Topbar MOD row still shows `IDLE · +50% → TIER` until player selects.
4. If player had queued a module, it auto-starts now.

### 6.3 Queueing Ahead

1. Player clicks `[≡ QUEUE]` on ACTIVE TIER block → queue section expands.
2. `[+ ADD TIER]` → overlay opens in queue mode.
3. Player selects next 2 tiers in order.
4. Active TIER completes → queue[0] auto-starts, queue shifts up.

### 6.4 Salvage-Driven Memory Update

1. Player salvages a Red T2 derelict.
2. Backend adds progress to the matching module(s) and a smaller amount to Red Weapons T2 tier (gap-dependent — if player is researching T1, bleedover to T2 is modest).
3. If the salvaged module is the currently-active MODULE, its topbar bar animates a one-time "step" (brief fill burst, 300ms).
4. If the module is not active, it appears in Banked Progress with source tag `[salv]`.
5. Recent Events entry: `● Salvage yielded research: Red Rail T2 +14%`.

### 6.5 Re-Selection Mid-Research

1. Player clicks `[CHANGE]` on ACTIVE MODULE block.
2. Overlay opens at current module's color.
3. Player selects a different module → confirmation tooltip near primary button: `Current module progress saved as banked.`
4. `[START MODULE RESEARCH]` → old module's progress retained in memory, new one becomes active. No penalty — memory is the whole point.

---

## 7. State Reference

### 7.1 Track States

| State | Topbar Bar | Topbar Label | RESEARCH Tab Block |
|---|---|---|---|
| Active (researching) | Filling, faction glow | Project name in glow | Full block with rate info |
| Active · near complete (>90%) | Pulsing fill | Name + low-key pulse | Block shows `~0:12` countdown |
| Paused | Desaturated fill at current % | Project name in `TextDim` | `[RESUME]` replaces `[PAUSE]` |
| Idle (nothing selected) | Empty | `IDLE — SELECT` (TIER) / `IDLE · +50% → TIER` (MOD) | Idle-state block with `[SELECT]` |
| Complete (transition, 0.8s) | Full at 100%, flashing | Project name briefly bright | Block briefly shows `COMPLETE`, then advances |

### 7.2 Tier Node States (Tech Tree)

| State | Symbol | Interactive? | Can Start? | Can Queue? |
|---|---|---|---|---|
| Completed | `✓` | Yes — shows module list | No | No |
| Active | `▶` | Yes — shows progress | No (already active) | No |
| Available | `○` | Yes — shows focus | Yes | Yes |
| Queued | `⧗` | Yes — shows position | No | Remove from queue |
| Locked (predecessor incomplete) | `·` | Tooltip only | No | No |

### 7.3 Module States (Tech Tree, within unlocked tier)

| State | Indicator | Can Start? | Can Queue? |
|---|---|---|---|
| Available (rolled, 0% banked) | `[AVAILABLE]` tag | Yes | Yes |
| Available (with memory) | `[AVAILABLE · 34%]` | Yes | Yes |
| Active | `[ACTIVE · 42%]` | No | No |
| Locked slot (not rolled) | `[LOCKED]` | No | No |
| Completed | `[RESEARCHED]` + checkmark | No | No |

---

## 8. Godot Implementation Notes

### 8.1 Scene Structure

```
GalaxyMapScreen
├── Topbar (updated)
│   └── CreditsResearchBlock   ← width now 220px
│       ├── CreditsRow         ← existing
│       ├── Divider
│       ├── TierTrackRow       ← new
│       ├── Divider
│       └── ModuleTrackRow     ← new
├── LeftPanel
│   └── TabContent
│       └── ResearchTab        ← new, replaces fleet list when active
│           ├── ActiveTierBlock
│           ├── ActiveModuleBlock
│           ├── QueuesSection
│           └── BankedProgressList
└── TechTreeOverlay            ← new, hidden by default
    ├── Header
    ├── ColorTabBar
    ├── TierMatrix
    └── FocusPanel
```

### 8.2 State Management

A single `ResearchState` autoload or service holds:
- `ActiveTierProject` (nullable)
- `ActiveModuleProject` (nullable)
- `TierQueue` (max 5)
- `ModuleQueue` (max 5)
- `ModuleMemory: Dictionary<ModuleId, ResearchMemory>`
- `TierMemory: Dictionary<TierId, ResearchMemory>`

Each memory record tracks total progress plus per-source breakdown for the Banked Progress source tags.

Topbar rows, ACTIVE blocks, tier matrix cells, and banked list items are **all subscribers** to the same `ResearchState.Changed` signal. Never duplicate state across UI nodes.

### 8.3 Reuse from Existing UI Spec

- Glass material: StyleBoxFlat as documented in §11 of the existing spec.
- Colors: all tokens sourced from §2 of existing spec. No new colors introduced.
- Fonts: Exo 2 / Barlow Condensed / Share Tech Mono only. Same weights.
- Click target minimums: 40px rows, 36px buttons, 44px tabs — inherited.

### 8.4 Animation Budget

Research UI adds exactly three animations to the project. All are low-frequency state cues, not decorative motion:

1. **Near-completion pulse** — 0.5s sine cycle on bar fill alpha, only when progress > 90%.
2. **Salvage step** — 300ms ease-out fill bump when a memory source updates an active track.
3. **Idle-tier nudge** — slow (4s interval) 3-cycle background pulse on idle TIER row, then ceases.

No other animations on research surfaces. Hover states are instant. Opening/closing the overlay uses a 120ms opacity fade only (no slide-in, no scale).

### 8.5 Data Binding Notes

- Topbar rate calculation runs at 2Hz (not every frame) — cheap enough and the eye can't distinguish finer updates on a 3px bar.
- Banked Progress list only re-queries when RESEARCH tab is actually visible.
- Tier Matrix cell states are computed lazily per color when that color tab is activated; caching invalidates on any `ResearchState.Changed` signal.

---

## 9. Deferred / Out of Scope (MVP)

These are acknowledged but not specified here; they belong to later design passes:

- **Tech sharing visualization** between allied empires
- **Research project abandonment** penalty (currently: no penalty, only memory-preservation)
- **Tier research path branching** (T4+ may introduce choice points within a tier)
- **Espionage targeting UI** — "steal research" target picker uses this data but the UI lives in the (future) Diplomacy/Espionage screen
- **T4 and T5 tiers** — matrix expands by column; focus panel layout unchanged
- **Mobile/touch layout** — everything here assumes desktop pointer input; tab-and-swipe variants are a later port problem